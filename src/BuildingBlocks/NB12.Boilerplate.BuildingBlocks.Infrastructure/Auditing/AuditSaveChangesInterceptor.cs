using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
//using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Domain.Auditing;
using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using System.Reflection;
using System.Text.Json;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing
{
    public sealed class AuditSaveChangesInterceptor(
    IAuditIntegrationEventFactory factory,
    IAuditContextAccessor ctx,
    JsonSerializerOptions json) : SaveChangesInterceptor //ILogger<AuditSaveChangesInterceptor> logger
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureApplyAndEnqueue(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CaptureApplyAndEnqueue(eventData.Context);
            return result;
        }

        private void CaptureApplyAndEnqueue(DbContext? db)
        {
            if (db is null) return;

            var auditCtx = ctx.GetCurrent();
            var actor = auditCtx.UserId ?? auditCtx.Email ?? "system";

            var entries = db.ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity)
                .Where(e => e.Entity is not OutboxMessage)
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            if (entries.Count == 0) return;

            // 1) Auditing Felder setzen
            foreach (var e in entries)
            {
                var aud = (IAuditableEntity)e.Entity;
                if (e.State == EntityState.Added) aud.SetCreated(auditCtx.OccurredAtUtc, actor);
                if (e.State == EntityState.Modified) aud.SetModified(auditCtx.OccurredAtUtc, actor);
            }

            // 2) Changes in AuditOutboxEntry überführen
            var outboxEntries = new List<AuditOutboxEntry>(entries.Count);

            foreach (var e in entries)
            {
                var entityType = e.Metadata.ClrType.FullName ?? e.Metadata.ClrType.Name;
                var entity = (IAuditableEntity)e.Entity;
                var entityId = entity.GetAuditEntityId();
                //var entityId = TryGetEntityId(e) ?? "<unknown>";
                var op = e.State.ToString();

                var changes = BuildPropertyChanges(e); // mit DoNotAudit Filter
                outboxEntries.Add(new AuditOutboxEntry(entityType, entityId, op, changes));
            }

            var envelope = new AuditOutboxEnvelope(
                Module: ResolveModuleName(db),
                Context: auditCtx,
                Entries: outboxEntries);

            var integrationEvent = factory.Create(envelope);

            db.Set<OutboxMessage>().Add(new OutboxMessage(
                new OutboxMessageId(integrationEvent.Id),
                integrationEvent.OccurredAtUtc,
                integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name,
                JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), json),
                attemptCount: 0,
                processedAtUtc: null,
                lastError: null));
        }

        private static string ResolveModuleName(DbContext db)
            => db.GetType().Name.Replace("DbContext", string.Empty);

        //private static string? TryGetEntityId(EntityEntry entry)
        //{
        //    var pk = entry.Metadata.FindPrimaryKey();
        //    if (pk is null) return null;

        //    var values = pk.Properties
        //        .Select(p => entry.Property(p.Name).CurrentValue ?? entry.Property(p.Name).OriginalValue)
        //        .Where(v => v is not null)
        //        .Select(ToStringSafe);

        //    var joined = string.Join("|", values!);
        //    return string.IsNullOrWhiteSpace(joined) ? null : joined;
        //}

        private static IReadOnlyCollection<AuditOutboxPropertyChange> BuildPropertyChanges(EntityEntry entry)
        {
            var changes = new List<AuditOutboxPropertyChange>();

            foreach (var p in entry.Properties)
            {
                if (p.Metadata.IsPrimaryKey())
                    continue;

                // Update: nur modified
                if (entry.State == EntityState.Modified && !p.IsModified)
                    continue;

                // DoNotAudit Filter
                var pi = p.Metadata.PropertyInfo;
                if (pi is not null && pi.GetCustomAttribute<DoNotAuditAttribute>() is not null)
                    continue;

                var oldVal = entry.State == EntityState.Added ? null : p.OriginalValue;
                var newVal = entry.State == EntityState.Deleted ? null : p.CurrentValue;

                changes.Add(new AuditOutboxPropertyChange(p.Metadata.Name, oldVal, newVal));
            }

            return changes;
        }

        private static string? ToStringSafe(object? value)
            => value?.ToString();
    }
}
