using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Interfaces
{
    public interface IAuditRetentionConfigProvider
    {
        Task<AuditRetentionConfigDto> GetAsync(CancellationToken ct);
    }
}
