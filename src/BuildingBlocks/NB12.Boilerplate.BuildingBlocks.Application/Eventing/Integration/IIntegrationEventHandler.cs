namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
    {
        Task Handle(TEvent @event, CancellationToken ct);
    }
}
