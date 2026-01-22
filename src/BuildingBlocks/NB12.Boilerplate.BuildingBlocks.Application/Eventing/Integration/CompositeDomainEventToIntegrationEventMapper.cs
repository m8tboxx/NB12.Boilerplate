using NB12.Boilerplate.BuildingBlocks.Domain.Events;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public sealed class CompositeDomainEventToIntegrationEventMapper(
    IEnumerable<IDomainEventToIntegrationEventMapper> mappers)
    {
        private readonly IReadOnlyList<IDomainEventToIntegrationEventMapper> _mappers =
        mappers.GroupBy(m => m.GetType()).Select(g => g.First()).ToList();

        public IReadOnlyList<IIntegrationEvent> MapAll(IDomainEvent domainEvent)
            => _mappers.SelectMany(m => m.Map(domainEvent)).ToList();
    }
}
