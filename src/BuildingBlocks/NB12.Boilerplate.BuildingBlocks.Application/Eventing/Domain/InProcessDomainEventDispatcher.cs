using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Domain.Events;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain
{
    public sealed class InProcessDomainEventDispatcher
        (IServiceScopeFactory scopeFactory,
        ILogger<InProcessDomainEventDispatcher> logger) : IDomainEventDispatcher
    {
        private static readonly ConcurrentDictionary<Type, Type> HandlerInterfaceTypeCache = new();
        private static readonly ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new();

        public async Task Dispatch(IEnumerable<IDomainEvent> events, CancellationToken ct)
        {
            if (events is null)
                return;

            var list = events as IList<IDomainEvent> ?? [.. events];

            if (list.Count == 0) 
                return;

            using var scope = scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            foreach (var domainEvent in list)
            {
                ct.ThrowIfCancellationRequested();

                var eventType = domainEvent.GetType();
                var handlerInterfaceType =HandlerInterfaceTypeCache.GetOrAdd(
                    eventType, 
                    static t => typeof(IDomainEventHandler<>).MakeGenericType(t));

                var handledMethod = HandleMethodCache.GetOrAdd(
                    handlerInterfaceType,
                    static iface => iface.GetMethod("Handle")
                    ?? throw new InvalidOperationException($"Domain handler missing Handle: {iface.Name}"));

                var handlers = serviceProvider.GetServices(handlerInterfaceType);

                foreach (var handler in handlers)
                {
                    if (handler is null)
                        continue;

                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        var taskObject = handledMethod.Invoke(handler, new object[] { domainEvent, ct });

                        if (taskObject is not Task task)
                            throw new InvalidOperationException(
                                $"Domain handler returned non-Task. Handler={handler.GetType().FullName} Event={eventType.FullName}");

                        await task.ConfigureAwait(false);
                    }
                    catch (TargetInvocationException tie) when ((tie.InnerException is not null))
                    {
                        logger.LogError(tie.InnerException,
                            "Domain event handler threw. EventType={EventType} Handler={HandlerType}",
                            eventType.FullName,
                            handler.GetType().FullName);

                        ExceptionDispatchInfo.Capture(tie.InnerException).Throw();

                        throw; // unreachable
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Domain event handler failed. EventType={EventType} Handler={HandlerType}",
                            eventType.FullName,
                            handler.GetType().FullName);

                        throw;
                    }
                }
            }
        }
    }
}
