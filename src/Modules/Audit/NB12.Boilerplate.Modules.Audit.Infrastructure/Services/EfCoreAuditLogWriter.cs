using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using System.Text.Json;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class EfCoreAuditLogWriter(AuditDbContext db, ILogger<EfCoreAuditLogWriter> logger) : IAuditLogWriter
    {
        public async Task WriteAsync(AuditableEntitiesChangedIntegrationEvent e, CancellationToken ct)
        {
            // Idempotenz: wenn Event schon verarbeitet -> raus
            // (Unique Index auf IntegrationEventId bleibt trotzdem empfohlen als "last line of defense")
            var alreadyProcessed = await db.AuditLogs
                .AsNoTracking()
                .AnyAsync(x => x.IntegrationEventId == e.Id, ct);

            if (alreadyProcessed)
            {
                logger.LogInformation("AUDIT: Event {EventId} already processed. Skipping.", e.Id);
                return;
            }

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

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Race-Condition: zwei Worker/Handler instanzen laufen parallel,
                // beide sehen "not processed" und versuchen zu inserten.
                // Unique Index auf IntegrationEventId fängt das ab.
                var existsNow = await db.AuditLogs
                    .AsNoTracking()
                    .AnyAsync(x => x.IntegrationEventId == e.Id, ct);

                if (existsNow)
                {
                    logger.LogWarning(ex, "AUDIT: Event {EventId} processed concurrently. Ignoring duplicate.", e.Id);
                    return;
                }

                throw;
            }
        }
    }
}
