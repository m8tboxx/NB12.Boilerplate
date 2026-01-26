namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsOutboxMessageDto(
        Guid Id,
        DateTime OccurredAtUtc,
        string Type,
        int AttemptCount,
        DateTime? ProcessedAtUtc,
        string? LastError,
        string? Content);
}
