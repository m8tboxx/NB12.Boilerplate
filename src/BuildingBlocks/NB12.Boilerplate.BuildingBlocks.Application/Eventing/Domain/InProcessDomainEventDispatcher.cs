using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Domain.Events;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain
{
    public sealed class InProcessDomainEventDispatcher(IServiceScopeFactory scopeFactory) : IDomainEventDispatcher
    {
        public async Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct)
        {
            var list = events?.ToList() ?? [];
            if (list.Count == 0) return;

            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            foreach (var e in list)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(e.GetType());
                var handlers = sp.GetServices(handlerType);

                foreach (var h in handlers)
                {
                    var method = handlerType.GetMethod("Handle")
                        ?? throw new InvalidOperationException($"Domain handler missing Handle: {handlerType.Name}");

                    await (Task)method.Invoke(h, new object[] { e, ct })!;
                }
            }
        }
    }
}
