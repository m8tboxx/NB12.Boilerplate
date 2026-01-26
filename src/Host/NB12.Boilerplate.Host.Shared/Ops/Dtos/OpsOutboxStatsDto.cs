namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsOutboxStatsDto(
        long Total,
        long Pending,
        long Failed,
        long Processed,
        long Locked,
        DateTime? OldestPendingOccurredAtUtc,
        DateTime? OldestFailedOccurredAtUtc);
}
