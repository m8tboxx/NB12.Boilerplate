namespace NB12.Boilerplate.Modules.Auth.Application.Responses
{
    public sealed record OutboxStatsDto(
        long Total,
        long Pending,
        long Failed,
        long Processed,
        long Locked,
        DateTime? OldestPendingOccurredAtUtc,
        DateTime? OldestFailedOccurredAtUtc);
}
