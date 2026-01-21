using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;

namespace NB12.Boilerplate.Modules.Audit.Application.Interfaces
{
    public interface IAuditLogWriter
    {
        Task WriteAsync(AuditableEntitiesChangedIntegrationEvent e, CancellationToken ct);
    }
}
