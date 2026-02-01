namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public sealed record OutboxAdminStatsDto(
        long Total,
        long Pending,
        long Failed,
        long Processed,
        long Locked,
        DateTime? OldestPendingOccurredAtUtc,
        DateTime? OldestFailedOccurredAtUtc);
}
