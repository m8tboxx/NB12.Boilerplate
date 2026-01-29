using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public sealed class InboxStatsCollectorHostedService<TDbContext>(
        IDbContextFactory<TDbContext> dbFactory,
        IOptions<InboxMonitoringOptions> options,
        ModuleInboxStatsState state,
        ILogger<InboxStatsCollectorHostedService<TDbContext>> logger) : BackgroundService
        where TDbContext : DbContext
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.Value.Enabled) return;

            var delay = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollSeconds));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var db = await dbFactory.CreateDbContextAsync(stoppingToken);

                    var stats = await db.GetMessageStoreStatsAsync<InboxMessage>(
                        nowUtc: DateTime.UtcNow,
                        timestampPropertyName: nameof(InboxMessage.ReceivedAtUtc),
                        ct: stoppingToken);

                    state.Update(new InboxStatsSnapshot(
                        Total: stats.Total,
                        Pending: stats.Pending,
                        Processed: stats.Processed,
                        Failed: stats.Failed,
                        Locked: stats.Locked,
                        LastUpdatedUtc: DateTime.UtcNow));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Inbox stats polling failed. Module={Module}", state.ModuleKey);
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
