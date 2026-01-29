using NB12.Boilerplate.BuildingBlocks.Application.Ids;

namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record InboxMessageDto(
        InboxMessageId Id,
        Guid IntegrationEventId,
        string HandlerName,
        string EventType,
        DateTime ReceivedAtUtc,
        DateTime? ProcessedAtUtc,
        int AttemptCount,
        string? LastError,
        DateTime? LastFailedAtUtc,
        DateTime? LockedUntilUtc,
        string? LockedOwner,
        DateTime? DeadLetteredAtUtc,
        string? DeadLetterReason);
}
