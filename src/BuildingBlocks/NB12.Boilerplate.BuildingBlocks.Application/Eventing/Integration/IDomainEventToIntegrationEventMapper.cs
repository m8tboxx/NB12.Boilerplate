using NB12.Boilerplate.BuildingBlocks.Domain.Events;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public interface IDomainEventToIntegrationEventMapper
    {
        IEnumerable<IIntegrationEvent> Map(IDomainEvent domainEvent);
    }
}
