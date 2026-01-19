using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Domain.Entities
{
    public sealed class AuditLog
    {
        public AuditLog(
            AuditLogId id,
            DateTime occurredAtUtc,
            string entityType,
            string entityId,
            string operation,
            string changesJson,
            string? traceId,
            string? correlationId,
            string? userId,
            string? email)
        {
            Id = id;
            OccurredAtUtc = occurredAtUtc;
            EntityType = entityType;
            EntityId = entityId;
            Operation = operation;
            ChangesJson = changesJson;
            TraceId = traceId;
            CorrelationId = correlationId;
            UserId = userId;
            Email = email;
        }
        public AuditLogId Id { get; private set; }
        public DateTime OccurredAtUtc { get; private set; }

        public string? UserId { get; private set; }
        public string? Email { get; private set; }
        public string? TraceId { get; private set; }
        public string? CorrelationId { get; private set; }

        public string EntityType { get; private set; } = default!;
        public string EntityId { get; private set; } = default!;
        public string Operation { get; private set; } = default!;

        // jsonb
        public string ChangesJson { get; private set; } = default!;
    }
}
