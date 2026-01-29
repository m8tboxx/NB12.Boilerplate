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
        private static readonly ConcurrentDictionary<Type, string> HandlerModuleKeyCache = new();

        public async Task Publish(IIntegrationEvent @event, CancellationToken ct)
        {
            if (@event is null)
                throw new ArgumentNullException(nameof(@event));

            using var scope = scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var inboxOptions = serviceProvider.GetRequiredService<IOptions<InboxOptions>>().Value;

            IInboxStoreResolver? resolver = null;
            if(inboxOptions.Enabled)
                resolver = serviceProvider.GetRequiredService<IInboxStoreResolver>();
            

            var jsonOptions = serviceProvider.GetRequiredService<System.Text.Json.JsonSerializerOptions>();
            var eventType = @event.GetType();
            var eventTypeName = eventType.FullName ?? eventType.Name;
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(@event, @event.GetType(), jsonOptions);

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

                var handlerType = handler.GetType();
                var handlerName = handler.GetType().FullName ?? handler.GetType().Name;

                if(!inboxOptions.Enabled)
                {
                    await ExecuteWithoutInbox(@event, ct, handleMethod, handler, handlerName, eventTypeName);
                    continue;
                }

                var moduleKey = HandlerModuleKeyCache.GetOrAdd(handlerType, static t => ResolveModuleKey(t));
                var inbox = resolver!.Get(moduleKey);

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

                var stopWatch = Stopwatch.StartNew();

                try
                {
                    var taskObject = handleMethod.Invoke(
                        handler, 
                        [@event, ct]);

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
                        "Integration handler threw. EventId={EventId} EventType={EventType} Handler={HandlerType} Module={Module}",
                        @event.Id,
                        eventType.FullName,
                        handlerType.FullName,
                        moduleKey);

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
                        "Integration handler failed. EventId={EventId} EventType={EventType} Handler={HandlerType} Module={Module}",
                        @event.Id,
                        eventType.FullName,
                        handlerType.FullName,
                        moduleKey);

                    throw;
                }
                finally
                {
                    stopWatch.Stop();
                    metrics.HandlerDuration(handlerName, stopWatch.Elapsed.TotalMilliseconds);
                }
            }
        }

        private async Task ExecuteWithoutInbox(
            IIntegrationEvent @event,
            CancellationToken ct,
            MethodInfo handleMethod,
            object handler,
            string handlerName,
            string eventTypeName)
        {
            var sw = Stopwatch.StartNew();
            metrics.HandlerAcquire(handlerName);

            try
            {
                var taskObject = handleMethod.Invoke(handler, new object[] { @event, ct });

                if (taskObject is not Task task)
                    throw new InvalidOperationException(
                        $"Integration handler returned non-Task. Handler={handler.GetType().FullName} Event={eventTypeName}");

                await task.ConfigureAwait(false);

                metrics.HandlerProcessed(handlerName);
            }
            catch (TargetInvocationException tie) when (tie.InnerException is not null)
            {
                metrics.HandlerFailed(handlerName);

                logger.LogError(tie.InnerException,
                    "Integration handler threw (inbox disabled). EventId={EventId} EventType={EventType} Handler={HandlerType}",
                    @event.Id,
                    eventTypeName,
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
                    eventTypeName,
                    handler.GetType().FullName);

                throw;
            }
            finally
            {
                sw.Stop();
                metrics.HandlerDuration(handlerName, sw.Elapsed.TotalMilliseconds);
            }
        }

        private static string ResolveModuleKey(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<IntegrationHandlerModuleAttribute>();
            if (attr is not null)
                return attr.ModuleKey;

            var ns = handlerType.Namespace ?? string.Empty;
            const string marker = ".Modules.";

            var idx = ns.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
                throw new InvalidOperationException(
                    $"Cannot infer module key for handler '{handlerType.FullName}'. Add [IntegrationHandlerModule(\"...\")] or use namespace pattern '...Modules.<Module>...'.");

            var start = idx + marker.Length;
            var end = ns.IndexOf('.', start);
            if (end < 0) end = ns.Length;

            var module = ns.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(module))
                throw new InvalidOperationException(
                    $"Cannot infer module key for handler '{handlerType.FullName}'. Invalid namespace '{ns}'.");

            return module;
        }
    }
}
