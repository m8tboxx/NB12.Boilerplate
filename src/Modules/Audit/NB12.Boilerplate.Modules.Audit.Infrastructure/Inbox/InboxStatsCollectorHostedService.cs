using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    public sealed class InboxStatsCollectorHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<InboxMonitoringOptions> options,
        InboxStatsState state,
        ILogger<InboxStatsCollectorHostedService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.Value.Enabled)
                return;

            var poll = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollSeconds));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

                    var now = DateTime.UtcNow;

                    // Single roundtrip aggregate query for Postgres
                    var sql = @"
                        SELECT
                            COUNT(*)::bigint AS total,
                            COUNT(*) FILTER (WHERE ""ProcessedAtUtc"" IS NULL)::bigint AS pending,
                            COUNT(*) FILTER (WHERE ""ProcessedAtUtc"" IS NOT NULL)::bigint AS processed,
                            COUNT(*) FILTER (WHERE ""ProcessedAtUtc"" IS NULL AND ""LastFailedAtUtc"" IS NOT NULL)::bigint AS failed,
                            COUNT(*) FILTER (WHERE ""ProcessedAtUtc"" IS NULL AND ""LockedUntilUtc"" IS NOT NULL AND ""LockedUntilUtc"" > {0})::bigint AS locked
                        FROM ""audit"".""InboxMessages""";

                    var row = await db.Database.SqlQueryRaw<InboxStatsRow>(sql, now)
                        .SingleAsync(stoppingToken);

                    state.Update(new InboxStatsSnapshot(
                        Total: row.total,
                        Pending: row.pending,
                        Processed: row.processed,
                        Failed: row.failed,
                        Locked: row.locked,
                        LastUpdatedUtc: DateTime.UtcNow));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "InboxStatsCollector failed.");
                }

                await Task.Delay(poll, stoppingToken);
            }
        }

        private sealed record InboxStatsRow(long total, long pending, long processed, long failed, long locked);
    }
}
