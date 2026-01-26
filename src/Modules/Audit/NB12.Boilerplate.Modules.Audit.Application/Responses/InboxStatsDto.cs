namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record InboxStatsDto(
        long Total,
        long Pending,
        long Failed,
        long Processed,
        long Locked,
        DateTime? OldestPendingReceivedAtUtc,
        DateTime? OldestFailedReceivedAtUtc
    );
}
