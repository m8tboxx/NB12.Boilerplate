using NB12.Boilerplate.BuildingBlocks.Application.Ids;

namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin
{
    public sealed record InboxAdminMessageDetailsDto(
        InboxMessageId Id,
        Guid IntegrationEventId,
        string HandlerName,
        string EventType,
        string PayloadJson,
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
