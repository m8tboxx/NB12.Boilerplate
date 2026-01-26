using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Options;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class AuditRetentionHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<AuditRetentionOptions> _options;
        private readonly ILogger<AuditRetentionHostedService> _logger;

        public AuditRetentionHostedService(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<AuditRetentionOptions> options,
            ILogger<AuditRetentionHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var o = _options.CurrentValue;
                var delay = TimeSpan.FromMinutes(Math.Max(1, o.RunEveryMinutes));

                try
                {
                    if (o.Enabled)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IAuditRetentionService>();

                        var res = await svc.RunCleanupAsync(DateTime.UtcNow, stoppingToken);

                        if (res.DeletedAuditLogs > 0 || res.DeletedErrorLogs > 0)
                        {
                            _logger.LogInformation(
                                "AuditRetention cleanup ran: deleted {DeletedAuditLogs} audit logs, {DeletedErrorLogs} error logs.",
                                res.DeletedAuditLogs, res.DeletedErrorLogs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AuditRetention cleanup failed.");
                }

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }
        }
    }
}
