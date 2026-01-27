using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record InboxMessageDetailsDto(
        InboxMessageId Id,
        Guid IntegrationEventId,
        string HandlerName,
        string EventType,
        string PayloadJson,
        DateTime ReceivedAtUtc,
        DateTime? ProcessedAtUtc,
        int AttemptCount,
        string? LastError,
        DateTime? LockedUntilUtc,
        string? LockedOwner,
        DateTime? DeadLetteredAtUtc,
        string? DeadLetterReason);
}
