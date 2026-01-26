using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    internal sealed class NoOpInboxStore : IInboxStore
    {
        public Task<bool> TryAcquireAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime utcNow,
            DateTime lockedUntilUtc,
            CancellationToken ct)
            => Task.FromResult(true);

        public Task MarkProcessedAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime processedAtUtc,
            CancellationToken ct)
            => Task.CompletedTask;

        public Task MarkFailedAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime failedAtUtc,
            string error,
            CancellationToken ct)
            => Task.CompletedTask;
    }
}
