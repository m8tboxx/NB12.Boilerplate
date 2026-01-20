using NB12.Boilerplate.BuildingBlocks.Domain.Events;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain
{
    public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
    {
        Task Handle(TEvent @event, CancellationToken ct);
    }
}
