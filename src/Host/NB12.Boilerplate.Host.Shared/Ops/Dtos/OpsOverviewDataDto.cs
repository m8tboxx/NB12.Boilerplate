namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsOverviewDataDto(
        OpsOutboxStatsDto Outbox,
        OpsInboxStatsDto Inbox,
        OpsRetentionDto Retention);
}
