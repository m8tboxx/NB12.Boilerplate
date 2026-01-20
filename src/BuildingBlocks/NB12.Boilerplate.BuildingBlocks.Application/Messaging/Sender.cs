using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;

namespace NB12.Boilerplate.BuildingBlocks.Application.Messaging
{
    /// <summary>
    /// Mini-Mediator: resolves handler + runs pipeline behaviors.
    /// </summary>
    public sealed class Sender(IServiceProvider sp) : ISender
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestType = request.GetType();
            var responseType = typeof(TResponse);

            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            var handler = sp.GetRequiredService(handlerType);

            var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
            var behaviors = sp.GetServices(behaviorType).Reverse().ToArray();

            Task<TResponse> HandlerDelegate()
            {
                var handle = handlerType.GetMethod("Handle")
                    ?? throw new InvalidOperationException($"Handler '{handlerType.Name}' has no Handle method.");

                return (Task<TResponse>)handle.Invoke(handler, new object[] { request, ct })!;
            }

            RequestHandlerDelegate<TResponse> next = HandlerDelegate;

            foreach (var behavior in behaviors)
            {
                var current = next;
                next = () =>
                {
                    var handle = behaviorType.GetMethod("Handle")
                        ?? throw new InvalidOperationException($"Behavior '{behaviorType.Name}' has no Handle method.");

                    return (Task<TResponse>)handle.Invoke(behavior, new object[] { request, current, ct })!;
                };
            }

            return await next();
        }
    }
}
