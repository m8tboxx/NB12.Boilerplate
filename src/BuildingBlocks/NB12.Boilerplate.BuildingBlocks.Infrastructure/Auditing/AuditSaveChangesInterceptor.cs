using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;
using System.Runtime.CompilerServices;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing
{
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IAuditStore _auditStore;
        private readonly IAuditContextAccessor _ctx;
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;

        // Snapshot pro DbContext-Instanz
        private static readonly ConditionalWeakTable<DbContext, List<EntityChangeAudit>> Pending
            = new();

        public AuditSaveChangesInterceptor(IAuditStore auditStore, IAuditContextAccessor ctx, ILogger<AuditSaveChangesInterceptor> logger)
        {
            _auditStore = auditStore;
            _ctx = ctx;
            _logger = logger;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            CaptureAndApply(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureAndApply(eventData.Context);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            try
            {
                _ = FlushAsync(eventData.Context, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Audits must not kill business commitments, but they must be visible.
                _logger.LogError(ex, "Audit flush failed after SaveChanges (sync).");
            }
            
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await FlushAsync(eventData.Context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit flush failed after SaveChangesAsync.");
            }
            
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            try
            {
                _ = WriteDbErrorAsync(eventData.Exception, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit error-log write failed in SaveChangesFailed (sync).");
            }
            
            base.SaveChangesFailed(eventData);
        }

        public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            try
            {
                await WriteDbErrorAsync(eventData.Exception, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit error-log write failed in SaveChangesFailedAsync.");
            }
            
            await base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        private void CaptureAndApply(DbContext? db)
        {
            if (db is null) return;

            var auditCtx = _ctx.GetCurrent();
            var actor = auditCtx.UserId ?? auditCtx.Email ?? "system";
            var now = auditCtx.OccurredAtUtc;

            var entries = db.ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditableEntity)
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            if (entries.Count == 0)
                return;

            // Setze Auditing Felder (Created/Modified)
            foreach (var e in entries)
            {
                var aud = (IAuditableEntity)e.Entity;

                if (e.State == EntityState.Added)
                    aud.SetCreated(now, actor);

                if (e.State == EntityState.Modified)
                    aud.SetModified(now, actor);
            }

            // Snapshot erzeugen (vor Save)
            var snapshot = new List<EntityChangeAudit>(entries.Count);
            foreach (var e in entries)
            {
                snapshot.Add(ToEntityAudit(e));
            }

            Pending.Remove(db);
            Pending.Add(db, snapshot);
        }

        private async Task FlushAsync(DbContext? db, CancellationToken ct)
        {
            if (db is null) return;

            if (!Pending.TryGetValue(db, out var entries) || entries.Count == 0)
                return;

            Pending.Remove(db);

            var auditCtx = _ctx.GetCurrent();
            await _auditStore.WriteEntityChangesAsync(entries, auditCtx, ct);
        }

        private async Task WriteDbErrorAsync(Exception ex, CancellationToken ct)
        {
            var ctx = _ctx.GetCurrent();
            await _auditStore.WriteErrorAsync(
                new ErrorAudit(
                    Message: "EF Core SaveChanges failed",
                    ExceptionType: ex.GetType().FullName,
                    StackTrace: ex.ToString(),
                    Path: null,
                    Method: null,
                    StatusCode: null),
                ctx,
                ct);
        }

        private static EntityChangeAudit ToEntityAudit(EntityEntry entry)
        {
            var entityType = entry.Metadata.ClrType.FullName ?? entry.Metadata.ClrType.Name;

            var id = TryGetPrimaryKey(entry) ?? TryGetIdProperty(entry.Entity) ?? "<unknown>";
            var op = entry.State switch
            {
                EntityState.Added => AuditOperation.Insert,
                EntityState.Modified => AuditOperation.Update,
                EntityState.Deleted => AuditOperation.Delete,
                _ => AuditOperation.Update
            };

            var changes = new List<PropertyChange>();

            foreach (var p in entry.Properties)
            {
                if (p.Metadata.IsPrimaryKey())
                    continue;

                // Bei Update nur modified Properties loggen
                if (entry.State == EntityState.Modified && !p.IsModified)
                    continue;

                // Navigations/Collections sind hier nicht drin; das sind nur scalar properties.
                var oldVal = entry.State == EntityState.Added ? null : ToStringSafe(p.OriginalValue);
                var newVal = entry.State == EntityState.Deleted ? null : ToStringSafe(p.CurrentValue);

                if (oldVal == newVal && entry.State == EntityState.Modified)
                    continue;

                changes.Add(new PropertyChange(p.Metadata.Name, oldVal, newVal));
            }

            return new EntityChangeAudit(
                EntityType: entityType,
                EntityId: id,
                Operation: op,
                Changes: changes);
        }

        private static string? TryGetPrimaryKey(EntityEntry entry)
        {
            var pk = entry.Metadata.FindPrimaryKey();
            if (pk is null) return null;

            var values = pk.Properties
                .Select(p => entry.Property(p.Name).CurrentValue ?? entry.Property(p.Name).OriginalValue)
                .Where(v => v is not null)
                .Select(ToStringSafe);

            var joined = string.Join("|", values!);
            return string.IsNullOrWhiteSpace(joined) ? null : joined;
        }

        private static string? TryGetIdProperty(object entity)
        {
            var prop = entity.GetType().GetProperty("Id");
            var val = prop?.GetValue(entity);
            return val is null ? null : ToStringSafe(val);
        }

        private static string? ToStringSafe(object? value)
            => value?.ToString();
    }
}
