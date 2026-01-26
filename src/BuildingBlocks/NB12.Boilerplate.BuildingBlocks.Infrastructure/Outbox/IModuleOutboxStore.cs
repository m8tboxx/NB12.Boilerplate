using NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public interface IModuleOutboxStore
    {
        string Module { get; }

        /// <summary>
        /// Atomically claims a batch of unprocessed outbox messages for a given lock owner.
        /// Uses database-level locking semantics to avoid double-claiming across multiple workers.
        /// </summary>
        Task<IReadOnlyList<OutboxMessage>> ClaimUnprocessed(
            int batchSize,
            string lockOwner,
            TimeSpan lockTtl,
            CancellationToken ct);
        Task MarkProcessed(OutboxMessageId id, string lockOwner, DateTime utcNow, CancellationToken ct);
        Task MarkFailed(OutboxMessageId id, string lockOwner, DateTime utcNow, Exception ex, OutboxFailurePlan plan, CancellationToken ct);
    }
}
