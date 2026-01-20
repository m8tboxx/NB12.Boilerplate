using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    public interface IEventBus
    {
        Task Publish(IIntegrationEvent @event, CancellationToken ct);
    }
}
