using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    public sealed class InMemoryEventBus(IServiceScopeFactory scopeFactory) : IEventBus
    {
        public async Task Publish(IIntegrationEvent @event, CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(@event.GetType());
            var handlers = sp.GetServices(handlerType);

            foreach (var h in handlers)
            {
                var method = handlerType.GetMethod("Handle")
                    ?? throw new InvalidOperationException($"Integration handler missing Handle: {handlerType.Name}");

                await (Task)method.Invoke(h, new object[] { @event, ct })!;
            }
        }
    }
}
