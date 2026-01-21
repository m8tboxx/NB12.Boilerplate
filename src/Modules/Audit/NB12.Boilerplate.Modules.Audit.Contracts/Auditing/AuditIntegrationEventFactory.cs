using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;

namespace NB12.Boilerplate.Modules.Audit.Contracts.Auditing
{
    public sealed class AuditIntegrationEventFactory : IAuditIntegrationEventFactory
    {
        public IIntegrationEvent Create(AuditOutboxEnvelope envelope)
        {
            var entries = envelope.Entries.Select(e =>
            new AuditableEntityChange(
                e.EntityType,
                e.EntityId,
                e.Operation,
                e.Changes.Select(c => new AuditPropertyChange(c.Name, c.Old, c.New)).ToList()
            )).ToList();

            return new AuditableEntitiesChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredAtUtc: envelope.Context.OccurredAtUtc,
                Module: envelope.Module,
                UserId: envelope.Context.UserId,
                Email: envelope.Context.Email,
                TraceId: envelope.Context.TraceId,
                CorrelationId: envelope.Context.CorrelationId,
                Entries: entries);
        }
    }
}
