using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Application.Interfaces
{
    public interface IAuditIntegrationEventFactory
    {
        IIntegrationEvent Create(AuditOutboxEnvelope envelope);
    }
}
