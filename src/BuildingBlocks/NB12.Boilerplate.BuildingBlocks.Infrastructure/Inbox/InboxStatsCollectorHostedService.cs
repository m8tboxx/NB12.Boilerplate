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
        IOptionsMonitor<InboxMonitoringOptions> options,
        ModuleInboxStatsState state,
        ILogger<InboxStatsCollectorHostedService<TDbContext>> logger) : BackgroundService
        where TDbContext : DbContext
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var opt = options.Get(state.ModuleKey);

                if (!opt.Enabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }
                    
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
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Inbox stats polling failed. Module={Module}", state.ModuleKey);
                }

                var delay = TimeSpan.FromSeconds(Math.Max(1, opt.PollSeconds));
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
