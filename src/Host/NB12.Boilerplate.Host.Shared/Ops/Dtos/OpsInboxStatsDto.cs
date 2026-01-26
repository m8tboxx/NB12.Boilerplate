namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsInboxStatsDto(
        long Total,
        long Pending,
        long Failed,
        long Processed,
        long Locked,
        DateTime? OldestPendingReceivedAtUtc,
        DateTime? OldestFailedReceivedAtUtc);
}
