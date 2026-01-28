namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    /// <summary>
    /// Consumer-side idempotency store for integration event handling (Inbox Pattern).
    /// Each handler should process a given IntegrationEventId at most once.
    /// </summary>
    public interface IInboxStore
    {
        /// <summary>
        /// Tries to acquire a processing lock for the given IntegrationEventId + handler.
        /// Returns false if it was already processed or currently locked by another worker.
        /// </summary>
        Task<bool> TryAcquireAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime utcNow,
            DateTime lockedUntilUtc,
            string eventType,
            string payloadJson,
            CancellationToken ct);

        /// <summary>
        /// Marks the given IntegrationEventId + handler as processed.
        /// Must be called by the same lock owner that acquired it.
        /// </summary>
        Task MarkProcessedAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime processedAtUtc,
            CancellationToken ct);

        /// <summary>
        /// Marks a handling attempt as failed and releases the processing lock.
        /// Must be called by the same lock owner that acquired it.
        /// </summary>
        Task MarkFailedAsync(
            Guid integrationEventId,
            string handlerName,
            string lockOwner,
            DateTime failedAtUtc,
            string error,
            CancellationToken ct);
    }
}
