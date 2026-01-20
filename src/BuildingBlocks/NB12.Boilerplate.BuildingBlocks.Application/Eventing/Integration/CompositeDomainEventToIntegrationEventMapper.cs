using NB12.Boilerplate.BuildingBlocks.Domain.Events;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public sealed class CompositeDomainEventToIntegrationEventMapper(
    IEnumerable<IDomainEventToIntegrationEventMapper> mappers)
    {
        public IReadOnlyList<IIntegrationEvent> MapAll(IDomainEvent domainEvent)
            => mappers.SelectMany(m => m.Map(domainEvent)).ToList();
    }
}
