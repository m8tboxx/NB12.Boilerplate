using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public sealed class InboxCleanupHostedService<TDbContext>(
        IDbContextFactory<TDbContext> dbFactory,
        IOptionsMonitor<InboxCleanupOptions> options,
        ModuleInboxStatsState state,
        ILogger<InboxCleanupHostedService<TDbContext>> logger) : BackgroundService
        where TDbContext : DbContext
    {
        private const int BatchSize = 5_000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var opt = options.Get(state.ModuleKey);

                if(!opt.Enabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                try
                {
                    await using var db = await dbFactory.CreateDbContextAsync(stoppingToken);
                    var utcNow = DateTime.UtcNow;

                    var processedCutoff = utcNow.AddDays(-Math.Max(1, opt.RetainProcessedDays));
                    var deletedProcessed = await DeleteProcessedBeforeAsync(db, processedCutoff, BatchSize, stoppingToken);

                    int deletedFailed = 0;
                    if (opt.RetainFailedDays > 0)
                    {
                        var failedCutoff = utcNow.AddDays(-opt.RetainFailedDays);
                        deletedFailed = await DeleteFailedBeforeAsync(db, failedCutoff, utcNow, BatchSize, stoppingToken);
                    }

                    if (deletedProcessed > 0 || deletedFailed > 0)
                        logger.LogInformation("Inbox cleanup completed. Module={Module} DeletedProcessed={DP} DeletedFailed={DF}",
                            state.ModuleKey, deletedProcessed, deletedFailed);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Inbox cleanup failed. Module={Module}", state.ModuleKey);
                }

                var delay = TimeSpan.FromMinutes(Math.Max(1, opt.RunEveryMinutes));
                await Task.Delay(delay, stoppingToken);
            }
        }

        private static async Task<int> DeleteProcessedBeforeAsync(
            TDbContext db, DateTime beforeUtc, int maxRows, CancellationToken ct)
        {
            var (table, storeId, et) = EfPostgresSql.Table<InboxMessage>(db);
            var processedAt = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.ProcessedAtUtc));

            var sql = $@"
                DELETE FROM {table}
                WHERE ctid IN (
                    SELECT ctid FROM {table}
                    WHERE {processedAt} IS NOT NULL AND {processedAt} < {{0}}
                    LIMIT {{1}}
                );";

            return await db.Database.ExecuteSqlRawAsync(sql, new object[] { beforeUtc, maxRows }, ct);
        }

        private static async Task<int> DeleteFailedBeforeAsync(
            TDbContext db, DateTime failedBeforeUtc, DateTime utcNow, int maxRows, CancellationToken ct)
        {
            var (table, storeId, et) = EfPostgresSql.Table<InboxMessage>(db);

            var processedAt = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.ProcessedAtUtc));
            var attempt = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.AttemptCount));
            var lastFailed = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LastFailedAtUtc));
            var receivedAt = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.ReceivedAtUtc));
            var lockedUntil = EfPostgresSql.Column(et, storeId, nameof(InboxMessage.LockedUntilUtc));

            var sql = $@"
                DELETE FROM {table}
                WHERE ctid IN (
                    SELECT ctid FROM {table}
                    WHERE {processedAt} IS NULL
                      AND {attempt} > 0
                      AND COALESCE({lastFailed}, {receivedAt}) < {{0}}
                      AND ({lockedUntil} IS NULL OR {lockedUntil} < {{1}})
                    LIMIT {{2}}
                );";

            return await db.Database.ExecuteSqlRawAsync(sql, new object[] { failedBeforeUtc, utcNow, maxRows }, ct);
        }
    }
}
