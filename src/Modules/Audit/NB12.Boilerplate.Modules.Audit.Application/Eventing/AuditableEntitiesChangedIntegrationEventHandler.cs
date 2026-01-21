using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;

namespace NB12.Boilerplate.Modules.Audit.Application.Eventing
{
    public sealed class AuditableEntitiesChangedIntegrationEventHandler(IAuditLogWriter writer)
    : IIntegrationEventHandler<AuditableEntitiesChangedIntegrationEvent>
    {
        public Task Handle(AuditableEntitiesChangedIntegrationEvent e, CancellationToken ct)
        => writer.WriteAsync(e, ct);
    }
}
