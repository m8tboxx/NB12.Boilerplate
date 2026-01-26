using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using System.Diagnostics;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    public sealed class InboxCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<InboxCleanupOptions> options,
        InboxMetrics metrics,
        ILogger<InboxCleanupHostedService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.Value.Enabled)
                return;

            var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.RunEveryMinutes));

            while (!stoppingToken.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                long deletedProcessed = 0;
                long deletedFailed = 0;

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

                    var now = DateTime.UtcNow;

                    // Delete processed older than retention
                    var retainProcessedDays = Math.Max(1, options.Value.RetainProcessedDays);
                    var cutoffProcessed = now.AddDays(-retainProcessedDays);

                    var deleteProcessedSql = @"
                        DELETE FROM ""audit"".""InboxMessages""
                        WHERE ""ProcessedAtUtc"" IS NOT NULL
                          AND ""ProcessedAtUtc"" < {0}";

                    deletedProcessed = await db.Database.ExecuteSqlRawAsync(
                        deleteProcessedSql, 
                        parameters: new object[] { cutoffProcessed }, 
                        cancellationToken: stoppingToken);

                    // Optionally delete failed/unprocessed older than retention (if configured)
                    var retainFailedDays = options.Value.RetainFailedDays;
                    if (retainFailedDays > 0)
                    {
                        var cutoffFailed = now.AddDays(-Math.Max(1, retainFailedDays));

                        var deleteFailedSql = @"
                            DELETE FROM ""audit"".""InboxMessages""
                            WHERE ""ProcessedAtUtc"" IS NULL
                              AND ""LastFailedAtUtc"" IS NOT NULL
                              AND ""LastFailedAtUtc"" < {0}
                              AND (""LockedUntilUtc"" IS NULL OR ""LockedUntilUtc"" < {1})";

                        deletedFailed = await db.Database.ExecuteSqlRawAsync(
                            deleteFailedSql,
                            parameters: new object[] { cutoffFailed, now },
                            cancellationToken: stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "InboxCleanup failed.");
                }
                finally
                {
                    sw.Stop();
                    metrics.CleanupRun(sw.Elapsed.TotalMilliseconds, deletedProcessed, deletedFailed);

                    logger.LogInformation(
                        "InboxCleanup completed. DeletedProcessed={DeletedProcessed} DeletedFailed={DeletedFailed} DurationMs={DurationMs}",
                        deletedProcessed, deletedFailed, sw.Elapsed.TotalMilliseconds);
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}
