using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using System.Text.Json;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    public sealed class EFCoreAuditStore : IAuditStore
    {
        private readonly AuditDbContext _dbContext;

        public EFCoreAuditStore(AuditDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task WriteEntityChangesAsync(
            IReadOnlyCollection<EntityChangeAudit> entries,
            AuditContext context,
            CancellationToken ct = default)
        {
            if (entries.Count == 0) return;

            foreach (var entry in entries)
            {
                _dbContext.AuditLogs.Add(new AuditLog
                (
                    AuditLogId.New(),
                    context.OccurredAtUtc,
                    entry.EntityType,
                    entry.EntityId,
                    entry.Operation.ToString(),
                    JsonSerializer.Serialize(entry.Changes),
                    context.TraceId,
                    context.CorrelationId, 
                    context.UserId,
                    context.Email
                ));
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task WriteErrorAsync(ErrorAudit error, AuditContext context, CancellationToken ct = default)
        {
            _dbContext.ErrorLogs.Add(new ErrorLog
            (
                ErrorLogId.New(),
                context.OccurredAtUtc,
                error.Message,
                context.UserId,
                context.Email,
                context.TraceId,
                context.CorrelationId,
                error.ExceptionType,
                error.StackTrace,
                error.Path,
                error.Method,
                error.StatusCode
            ));

            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
