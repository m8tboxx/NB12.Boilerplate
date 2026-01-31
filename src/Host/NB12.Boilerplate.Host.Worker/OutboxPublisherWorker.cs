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
        private readonly string _workerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batchSize = Math.Max(1, options.Value.BatchSize);
            var delay = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollSeconds));

            // Claim TTL: should be larger than typical publish time.
            var lockSeconds = Math.Max(10, options.Value.LockSeconds);
            var lockTtl = TimeSpan.FromSeconds(lockSeconds);

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
                        var msgs = await store.ClaimUnprocessed(batchSize, _workerId, lockTtl, stoppingToken);

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
                                await store.MarkProcessed(msg.Id, _workerId, DateTime.UtcNow, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                var now = DateTime.UtcNow;

                                // Decide retry vs dead-letter based on attempt count (msg.AttemptCount from DB)
                                var nextAttempt = msg.AttemptCount + 1;

                                OutboxFailurePlan plan;
                                if (nextAttempt >= options.Value.MaxAttempts)
                                {
                                    plan = OutboxFailurePlan.DeadLetter($"max_attempts_reached:{options.Value.MaxAttempts}");
                                }
                                else
                                {
                                    var backoffSeconds = ComputeBackoffSeconds(
                                        baseSeconds: options.Value.BaseRetrySeconds,
                                        maxSeconds: options.Value.MaxRetrySeconds,
                                        attemptNumber: nextAttempt);

                                    plan = OutboxFailurePlan.Retry(now.AddSeconds(backoffSeconds));
                                }

                                logger.LogError(ex,
                                    "Outbox publish failed. Module={Module} MsgId={MsgId} Type={Type} Attempt={Attempt} Plan={Plan} Worker={WorkerId}",
                                    store.ModuleKey, msg.Id, msg.Type, nextAttempt, plan.Action, _workerId);

                                await store.MarkFailed(
                                    msg.Id,
                                    _workerId,
                                    now,
                                    ex,
                                    plan,
                                    stoppingToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Outbox loop crashed. Worker={WorkerId}", _workerId);
                }

                await Task.Delay(delay, stoppingToken);
            }
        }

        private static int ComputeBackoffSeconds(int baseSeconds, int maxSeconds, int attemptNumber)
        {
            baseSeconds = Math.Max(1, baseSeconds);
            maxSeconds = Math.Max(baseSeconds, maxSeconds);
            attemptNumber = Math.Max(1, attemptNumber);

            // Exponential backoff: base * 2^(attempt-1)
            // attemptNumber is the *next* attempt (1..n)
            var factor = Math.Pow(2, attemptNumber - 1);
            var seconds = (int)Math.Round(baseSeconds * factor);

            return Math.Min(maxSeconds, seconds);
        }
    }

    public sealed record OutboxOptions
    {
        public int BatchSize { get; init; } = 20;
        public int PollSeconds { get; init; } = 2;

        // Claim TTL (seconds) for in-flight processing
        public int LockSeconds { get; init; } = 30;

        // Retry policy
        public int MaxAttempts { get; init; } = 10;
        public int BaseRetrySeconds { get; init; } = 5;
        public int MaxRetrySeconds { get; init; } = 300;
    }
}
