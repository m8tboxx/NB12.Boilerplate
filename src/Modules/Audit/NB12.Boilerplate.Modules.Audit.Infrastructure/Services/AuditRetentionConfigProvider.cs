using Microsoft.Extensions.Options;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Options;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class AuditRetentionConfigProvider(IOptionsMonitor<AuditRetentionOptions> options)
        : IAuditRetentionConfigProvider
    {
        public Task<AuditRetentionConfigDto> GetAsync(CancellationToken ct)
        {
            var o = options.CurrentValue;

            var dto = new AuditRetentionConfigDto(
                Enabled: o.Enabled,
                RunEveryMinutes: o.RunEveryMinutes,
                RetainAuditLogsDays: o.RetainAuditLogsDays,
                RetainErrorLogsDays: o.RetainErrorLogsDays);

            return Task.FromResult(dto);
        }
    }
}
