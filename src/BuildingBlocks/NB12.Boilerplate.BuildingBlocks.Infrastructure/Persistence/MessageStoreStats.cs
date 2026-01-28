namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence
{
    public readonly record struct MessageStoreStats(
        long Total,
        long Pending,
        long Failed,
        long Processed,
        long Locked,
        DateTime? OldestPendingUtc,
        DateTime? OldestFailedUtc);
}
