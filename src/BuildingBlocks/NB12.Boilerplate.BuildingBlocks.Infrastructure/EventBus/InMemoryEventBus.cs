using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    public sealed class InMemoryEventBus(
        IServiceScopeFactory scopeFactory, 
        InboxMetrics metrics,
        ILogger<InMemoryEventBus> logger) : IEventBus
    {
        private readonly string _busInstanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

        private static readonly ConcurrentDictionary<Type, Type> HandlerInterfaceTypeCache = new();
        private static readonly ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new();

        public async Task Publish(IIntegrationEvent @event, CancellationToken ct)
        {
            if (@event is null)
                throw new ArgumentNullException(nameof(@event));

            using var scope = scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var inbox = serviceProvider.GetRequiredService<IInboxStore>();
            var inboxOptions = serviceProvider.GetRequiredService<IOptions<InboxOptions>>().Value;

            var json = serviceProvider.GetRequiredService<System.Text.Json.JsonSerializerOptions>();
            var eventType = @event.GetType();
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(@event, @event.GetType(), json);

            var handlerInterfaceType = HandlerInterfaceTypeCache.GetOrAdd(
                eventType,
                static t => typeof(IIntegrationEventHandler<>).MakeGenericType(t));

            var handleMethod = HandleMethodCache.GetOrAdd(
                handlerInterfaceType,
                static iface => iface.GetMethod("Handle")
                ?? throw new InvalidOperationException($"Integration handler missing Handle: {iface.Name}"));

            var handlers = serviceProvider.GetServices(handlerInterfaceType);

            foreach (var handler in handlers)
            {
                ct.ThrowIfCancellationRequested();

                if (handler is null)
                    continue;

                var handlerName = handler.GetType().FullName ?? handler.GetType().Name;

                if (!inboxOptions.Enabled)
                {
                    var stopWatch = Stopwatch.StartNew();
                    metrics.HandlerAcquire(handlerName);

                    try
                    {
                        var taskObject = handleMethod.Invoke(handler, new object[] { @event, ct });

                        if (taskObject is not Task task)
                            throw new InvalidOperationException(
                                $"Integration handler returned non-Task. Handler={handler.GetType().FullName} Event={eventType.FullName}");

                        await task.ConfigureAwait(false);

                        metrics.HandlerProcessed(handlerName);
                    }
                    catch (TargetInvocationException tie) when (tie.InnerException is not null)
                    {
                        metrics.HandlerFailed(handlerName);

                        logger.LogError(tie.InnerException,
                            "Integration handler threw (inbox disabled). EventId={EventId} EventType={EventType} Handler={HandlerType}",
                            @event.Id,
                            eventType.FullName,
                            handler.GetType().FullName);

                        ExceptionDispatchInfo.Capture(tie.InnerException).Throw();

                        throw; // unreachable
                    }
                    catch (Exception ex)
                    {
                        metrics.HandlerFailed(handlerName);

                        logger.LogError(ex,
                            "Integration handler failed (inbox disabled). EventId={EventId} EventType={EventType} Handler={HandlerType}",
                            @event.Id,
                            eventType.FullName,
                            handler.GetType().FullName);

                        throw;
                    }
                    finally
                    {
                        stopWatch.Stop();
                        metrics.HandlerDuration(handlerName, stopWatch.Elapsed.TotalMilliseconds);
                    }

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
                    eventType: eventType.FullName ?? eventType.Name,
                    payloadJson: payloadJson, 
                    ct: ct);

                if (!acquired)
                {
                    metrics.HandlerDuplicateSkip(handlerName);
                    continue;
                }

                var stopWatchInbox = Stopwatch.StartNew();

                try
                {
                    var taskObject = handleMethod.Invoke(null, new object[] { @event, ct });

                    if(taskObject is not Task task)
                        throw new InvalidOperationException(
                            $"Integration handler returned non-Task. Handler={handler.GetType().FullName} Event={eventType.FullName}");

                    await task.ConfigureAwait(false);

                    await inbox.MarkProcessedAsync(
                        @event.Id,
                        handlerName,
                        _busInstanceId,
                        processedAtUtc: DateTime.UtcNow,
                        ct: ct).ConfigureAwait(false);

                    metrics.HandlerProcessed(handlerName);
                }
                catch (TargetInvocationException tie) when (tie.InnerException is not null)
                {
                    var inner = tie.InnerException;

                    await inbox.MarkFailedAsync(
                        @event.Id,
                        handlerName,
                        _busInstanceId,
                        failedAtUtc: DateTime.UtcNow,
                        error: inner.ToString(),
                        ct: ct).ConfigureAwait(false);

                    metrics.HandlerFailed(handlerName);

                    logger.LogError(inner,
                        "Integration handler threw. EventId={EventId} EventType={EventType} Handler={HandlerType}",
                        @event.Id,
                        eventType.FullName,
                        handler.GetType().FullName);

                    ExceptionDispatchInfo.Capture(inner).Throw();

                    throw; // unreachable
                }
                catch (Exception ex)
                {
                    await inbox.MarkFailedAsync(
                        @event.Id,
                        handlerName,
                        _busInstanceId,
                        failedAtUtc: DateTime.UtcNow,
                        error: ex.ToString(),
                        ct: ct).ConfigureAwait(false);

                    metrics.HandlerFailed(handlerName);

                    logger.LogError(ex,
                        "Integration handler failed. EventId={EventId} EventType={EventType} Handler={HandlerType}",
                        @event.Id,
                        eventType.FullName,
                        handler.GetType().FullName);

                    throw;
                }
                finally
                {
                    stopWatchInbox.Stop();
                    metrics.HandlerDuration(handlerName, stopWatchInbox.Elapsed.TotalMilliseconds);
                }
            }
        }
    }
}
