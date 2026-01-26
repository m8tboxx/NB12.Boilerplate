using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using System.Text.Json;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class EfCoreAuditLogWriter(AuditDbContext db) : IAuditLogWriter
    {
        public async Task WriteAsync(AuditableEntitiesChangedIntegrationEvent e, CancellationToken ct)
        {
            foreach (var entry in e.Entries)
            {
                db.AuditLogs.Add(new AuditLog(
                    id: AuditLogId.New(),
                    integrationEventId: e.Id,
                    occurredAtUtc: e.OccurredAtUtc,
                    module: e.Module,
                    entityType: entry.EntityType,
                    entityId: entry.EntityId,
                    operation: entry.Operation,
                    changesJson: JsonSerializer.Serialize(entry.Changes),
                    traceId: e.TraceId,
                    correlationId: e.CorrelationId,
                    userId: e.UserId,
                    email: e.Email));
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
