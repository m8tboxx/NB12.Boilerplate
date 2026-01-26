namespace NB12.Boilerplate.Modules.Auth.Application.Responses
{
    public sealed record OutboxMessageDetailsDto(
        Guid Id,
        DateTime OccurredAtUtc,
        string Type,
        string Content,
        int AttemptCount,
        DateTime? ProcessedAtUtc,
        string? LastError
    );
}
