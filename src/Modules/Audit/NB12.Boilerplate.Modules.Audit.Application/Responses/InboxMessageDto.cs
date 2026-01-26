namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record InboxMessageDto(
        Guid IntegrationEventId,
        string HandlerName,
        DateTime ReceivedAtUtc,
        DateTime? ProcessedAtUtc,
        int AttemptCount,
        DateTime? LastFailedAtUtc,
        string? LastError,
        DateTime? LockedUntilUtc,
        string? LockedOwner
    );
}
