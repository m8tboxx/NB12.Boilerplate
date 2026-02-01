using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public sealed class OutboxCleanupHostedService<TDbContext>(
        IDbContextFactory<TDbContext> dbFactory,
        IOptionsMonitor<OutboxCleanupOptions> options,
        string moduleKey,
        ILogger<OutboxCleanupHostedService<TDbContext>> logger) : BackgroundService
        where TDbContext : DbContext
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var opt = options.Get(moduleKey);
                if (!opt.Enabled) return;

                var delay = TimeSpan.FromMinutes(Math.Max(1, opt.RunEveryMinutes));
                var batch = Math.Max(1, opt.BatchSize);

                try
                {
                    await using var db = await dbFactory.CreateDbContextAsync(stoppingToken);
                    var utcNow = DateTime.UtcNow;

                    var deletedProcessed = 0;
                    var deletedDead = 0;
                    var deletedFailed = 0;

                    // Processed
                    if (opt.RetainPublishedDays > 0)
                    {
                        var cutoff = utcNow.AddDays(-opt.RetainPublishedDays);
                        deletedProcessed = await DeleteProcessedBeforeAsync(db, cutoff, batch, stoppingToken);
                    }

                    // Deadletters
                    if (opt.RetainDeadLetterDays > 0)
                    {
                        var cutoff = utcNow.AddDays(-opt.RetainDeadLetterDays);
                        deletedDead = await DeleteDeadLetteredBeforeAsync(db, cutoff, batch, stoppingToken);
                    }

                    // Optional: Failed (unprocessed, not deadlettered)
                    if (opt.RetainFailedDays > 0)
                    {
                        var cutoff = utcNow.AddDays(-opt.RetainFailedDays);
                        deletedFailed = await DeleteFailedBeforeAsync(db, cutoff, utcNow, batch, stoppingToken);
                    }

                    if (deletedProcessed > 0 || deletedDead > 0 || deletedFailed > 0)
                    {
                        logger.LogInformation(
                            "Outbox cleanup completed. Module={Module} DeletedProcessed={DP} DeletedDeadLettered={DD} DeletedFailed={DF}",
                            moduleKey, deletedProcessed, deletedDead, deletedFailed);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Outbox cleanup failed. Module={Module}", moduleKey);
                }

                await Task.Delay(delay, stoppingToken);
            }
        }

        private static async Task<int> DeleteProcessedBeforeAsync(
            TDbContext db, DateTime beforeUtc, int maxRows, CancellationToken ct)
        {
            var (table, storeId, et) = EfPostgresSql.Table<OutboxMessage>(db);

            var processedAt = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.ProcessedAtUtc));

            var sql = $@"
                DELETE FROM {table}
                WHERE ctid IN (
                    SELECT ctid FROM {table}
                    WHERE {processedAt} IS NOT NULL AND {processedAt} < {{0}}
                    LIMIT {{1}}
                );";

            return await db.Database.ExecuteSqlRawAsync(sql, new object[] { beforeUtc, maxRows }, ct);
        }

        private static async Task<int> DeleteDeadLetteredBeforeAsync(
            TDbContext db, DateTime beforeUtc, int maxRows, CancellationToken ct)
        {
            var (table, storeId, et) = EfPostgresSql.Table<OutboxMessage>(db);

            var deadLetteredAt = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.DeadLetteredAtUtc));
            var lockedBy = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.LockedBy));

            var sql = $@"
                DELETE FROM {table}
                WHERE ctid IN (
                    SELECT ctid FROM {table}
                    WHERE {deadLetteredAt} IS NOT NULL AND {deadLetteredAt} < {{0}}
                      AND {lockedBy} IS NULL
                    LIMIT {{1}}
                );";

            return await db.Database.ExecuteSqlRawAsync(sql, new object[] { beforeUtc, maxRows }, ct);
        }

        private static async Task<int> DeleteFailedBeforeAsync(
            TDbContext db, DateTime occurredBeforeUtc, DateTime utcNow, int maxRows, CancellationToken ct)
        {
            var (table, storeId, et) = EfPostgresSql.Table<OutboxMessage>(db);

            var processedAt = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.ProcessedAtUtc));
            var deadLetteredAt = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.DeadLetteredAtUtc));
            var attempt = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.AttemptCount));
            var occurredAt = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.OccurredAtUtc));
            var lockedUntil = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.LockedUntilUtc));
            var lockedBy = EfPostgresSql.Column(et, storeId, nameof(OutboxMessage.LockedBy));

            // Nur wenn NICHT locked und NICHT "für Retry in der Zukunft" versteckt:
            // - lockedBy null
            // - lockedUntil null ODER < now (sichtbar)
            var sql = $@"
                DELETE FROM {table}
                WHERE ctid IN (
                    SELECT ctid FROM {table}
                    WHERE {processedAt} IS NULL
                      AND {deadLetteredAt} IS NULL
                      AND {attempt} > 0
                      AND {occurredAt} < {{0}}
                      AND {lockedBy} IS NULL
                      AND ({lockedUntil} IS NULL OR {lockedUntil} < {{1}})
                    LIMIT {{2}}
                );";

            return await db.Database.ExecuteSqlRawAsync(sql, new object[] { occurredBeforeUtc, utcNow, maxRows }, ct);
        }
    }
}
