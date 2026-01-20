using NB12.Boilerplate.BuildingBlocks.Domain.Events;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain
{
    public interface IDomainEventDispatcher
    {
        Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct);
    }
}
