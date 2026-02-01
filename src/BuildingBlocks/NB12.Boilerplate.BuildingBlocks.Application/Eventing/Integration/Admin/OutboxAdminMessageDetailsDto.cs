namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public sealed record OutboxAdminMessageDetailsDto(
        Guid Id,
        DateTime OccurredAtUtc,
        string Type,
        string Content,
        int AttemptCount,
        DateTime? ProcessedAtUtc,
        string? LastError,
        DateTimeOffset? LockedUntilUtc,
        string? LockedBy,
        DateTime? DeadLetteredAtUtc,
        string? DeadLetterReason);
}
