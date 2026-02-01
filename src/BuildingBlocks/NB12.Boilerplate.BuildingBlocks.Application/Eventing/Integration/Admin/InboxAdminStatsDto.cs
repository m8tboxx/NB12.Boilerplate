namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public sealed record InboxAdminStatsDto(
        long Total,
        long Pending,
        long Failed,
        long Processed,
        long Locked,
        DateTime? OldestPendingReceivedAtUtc,
        DateTime? OldestFailedReceivedAtUtc);
}
