using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using System.Diagnostics;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    public sealed class InMemoryEventBus(IServiceScopeFactory scopeFactory, InboxMetrics metrics) : IEventBus
    {
        private readonly string _busInstanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

        public async Task Publish(IIntegrationEvent @event, CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var inbox = sp.GetRequiredService<IInboxStore>();
            var inboxOptions = sp.GetRequiredService<IOptions<InboxOptions>>().Value;

            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(@event.GetType());
            var handlers = sp.GetServices(handlerType);

            var method = handlerType.GetMethod("Handle")
                ?? throw new InvalidOperationException($"Integration handler missing Handle: {handlerType.Name}");

            foreach (var h in handlers)
            {
                if (h is null)
                    continue;

                var handlerName = h.GetType().FullName ?? h.GetType().Name;

                if (!inboxOptions.Enabled)
                {
                    var swNoInbox = Stopwatch.StartNew();
                    await (Task)method.Invoke(h, [@event, ct])!;
                    swNoInbox.Stop();

                    metrics.HandlerAcquire(handlerName);
                    metrics.HandlerProcessed(handlerName);
                    metrics.HandlerDuration(handlerName, swNoInbox.Elapsed.TotalMilliseconds);
                    continue;
                }

                var now = DateTime.UtcNow;
                var lockedUntilUtc = now.AddSeconds(Math.Max(1, inboxOptions.LockSeconds));

                metrics.HandlerAcquire(handlerName);

                var acquired = await inbox.TryAcquireAsync(
                    integrationEventId: @event.Id,
                    handlerName: handlerName,
                    lockOwner: _busInstanceId,
                    utcNow: now,
                    lockedUntilUtc: lockedUntilUtc,
                    ct: ct);

                if (!acquired)
                {
                    metrics.HandlerDuplicateSkip(handlerName);
                    continue;
                }

                var sw = Stopwatch.StartNew();
                try
                {
                    await (Task)method.Invoke(h, [@event, ct])!;
                    await inbox.MarkProcessedAsync(@event.Id, handlerName, _busInstanceId, DateTime.UtcNow, ct);

                    metrics.HandlerProcessed(handlerName);
                }
                catch (Exception ex)
                {
                    await inbox.MarkFailedAsync(@event.Id, handlerName, _busInstanceId, DateTime.UtcNow, ex.ToString(), ct);
                    metrics.HandlerFailed(handlerName);
                    throw;
                }
                finally
                {
                    sw.Stop();
                    metrics.HandlerDuration(handlerName, sw.Elapsed.TotalMilliseconds);
                }
            }
        }
    }
}
