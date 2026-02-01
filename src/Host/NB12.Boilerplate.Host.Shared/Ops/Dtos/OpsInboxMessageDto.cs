namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsInboxMessageDto(
        Guid Id,
        Guid IntegrationEventId,
        string HandlerName,
        string EventType,
        DateTime ReceivedAtUtc,
        DateTime? ProcessedAtUtc,
        int AttemptCount,
        string? LastError,
        string? PayloadJson);
}
