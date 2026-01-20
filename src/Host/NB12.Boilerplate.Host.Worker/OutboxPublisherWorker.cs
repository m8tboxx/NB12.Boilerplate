using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using System.Text.Json;

namespace NB12.Boilerplate.Host.Worker
{
    public sealed class OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisherWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batchSize = Math.Max(1, options.Value.BatchSize);
            var delay = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollSeconds));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var stores = scope.ServiceProvider.GetServices<IModuleOutboxStore>();
                    var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
                    var registry = scope.ServiceProvider.GetRequiredService<IntegrationEventTypeRegistry>();
                    var json = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();

                    foreach (var store in stores)
                    {
                        var msgs = await store.GetUnprocessed(batchSize, stoppingToken);

                        foreach (var msg in msgs)
                        {
                            try
                            {
                                if (!registry.TryResolve(msg.Type, out var eventType))
                                    throw new InvalidOperationException($"Unknown integration event type: '{msg.Type}'.");

                                var evt = (IIntegrationEvent?)JsonSerializer.Deserialize(msg.Content, eventType, json);
                                if (evt is null)
                                    throw new InvalidOperationException($"Deserialization returned null for '{msg.Type}'.");

                                await bus.Publish(evt, stoppingToken);
                                await store.MarkProcessed(msg, DateTime.UtcNow, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex,
                                    "Outbox publish failed. Module={Module} MsgId={MsgId} Type={Type}",
                                    store.Module, msg.Id, msg.Type);

                                await store.MarkFailed(msg, DateTime.UtcNow, ex, stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Outbox loop crashed.");
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    public sealed record OutboxOptions
    {
        public int BatchSize { get; init; } = 20;
        public int PollSeconds { get; init; } = 2;
    }
}
