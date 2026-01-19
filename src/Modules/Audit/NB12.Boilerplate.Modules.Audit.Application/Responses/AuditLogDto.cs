using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record AuditLogDto(
        AuditLogId Id,
        DateTime OccurredAtUtc,
        string? UserId,
        string? Email,
        string? TraceId,
        string? CorrelationId,
        string EntityType,
        string EntityId,
        string Operation,
        string ChangesJson);
}
