namespace NB12.Boilerplate.Modules.Auth.Application.Responses
{
    public sealed record OutboxMessageDto(
        Guid Id,
        DateTime OccurredAtUtc,
        string Type,
        int AttemptCount,
        DateTime? ProcessedAtUtc,
        string? LastError
    );
}
