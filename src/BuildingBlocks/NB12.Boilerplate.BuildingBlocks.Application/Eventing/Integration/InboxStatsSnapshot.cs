namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public sealed record InboxStatsSnapshot(
        long Total,
        long Pending,
        long Processed,
        long Failed,
        long Locked,
        DateTime LastUpdatedUtc);
}
