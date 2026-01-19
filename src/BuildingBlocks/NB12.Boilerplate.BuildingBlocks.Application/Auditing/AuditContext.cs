namespace NB12.Boilerplate.BuildingBlocks.Application.Auditing
{
    public sealed record AuditContext(
        DateTime OccurredAtUtc,
        string? UserId,
        string? Email,
        string? TraceId,
        string? CorrelationId);
}
