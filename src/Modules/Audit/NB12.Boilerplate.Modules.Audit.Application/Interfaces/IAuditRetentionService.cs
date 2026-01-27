using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Interfaces
{
    public interface IAuditRetentionService
    {
        AuditRetentionConfigDto GetConfig();

        Task<AuditRetentionCleanupResultDto> RunCleanupAsync(DateTime utcNow, CancellationToken ct);
        Task RunOnceAsync(CancellationToken ct);
    }
}
