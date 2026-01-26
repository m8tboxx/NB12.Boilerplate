using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Responses;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Repositories
{
    internal sealed class OutboxAdminRepository : IOutboxAdminRepository
    {
        private readonly IDbContextFactory<AuthDbContext> _dbFactory;

        public OutboxAdminRepository(IDbContextFactory<AuthDbContext> dbFactory) 
            => _dbFactory = dbFactory;

        public async Task<PagedResponse<OutboxMessageDto>> GetPagedAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            OutboxMessageState state,
            PageRequest page,
            Sort sort,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            IQueryable<OutboxMessage> q = db.Set<OutboxMessage>().AsNoTracking();

            q = ApplyFilters(q, fromUtc, toUtc, type, state);

            var total = await q.LongCountAsync(ct);

            var ordered = ApplySort(q, sort);

            var items = await ordered
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(x => new OutboxMessageDto(
                    x.Id.Value,
                    x.OccurredAtUtc,
                    x.Type,
                    x.AttemptCount,
                    x.ProcessedAtUtc,
                    x.LastError))
                .ToListAsync(ct);

            return new PagedResponse<OutboxMessageDto>(items, page.Page, page.PageSize, total);
        }

        public async Task<PagedResponse<OutboxMessageDetailsDto>> GetPagedWithDetailsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            OutboxMessageState state,
            PageRequest page,
            Sort sort,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            IQueryable<OutboxMessage> q = db.Set<OutboxMessage>().AsNoTracking();

            q = ApplyFilters(q, fromUtc, toUtc, type, state);

            var total = await q.LongCountAsync(ct);

            var ordered = ApplySort(q, sort);

            var items = await ordered
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(x => new OutboxMessageDetailsDto(
                    x.Id.Value,
                    x.OccurredAtUtc,
                    x.Type,
                    x.Content,
                    x.AttemptCount,
                    x.ProcessedAtUtc,
                    x.LastError))
                .ToListAsync(ct);

            return new PagedResponse<OutboxMessageDetailsDto>(items, page.Page, page.PageSize, total);
        }

        public async Task<OutboxMessageDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var key = new OutboxMessageId(id);

            return await db.Set<OutboxMessage>()
                .AsNoTracking()
                .Where(x => x.Id == key)
                .Select(x => new OutboxMessageDetailsDto(
                    x.Id.Value,
                    x.OccurredAtUtc,
                    x.Type,
                    x.Content,
                    x.AttemptCount,
                    x.ProcessedAtUtc,
                    x.LastError))
                .SingleOrDefaultAsync(ct);
        }

        public async Task<OutboxStatsDto> GetStatsAsync(CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var now = DateTime.UtcNow;

            var q = db.Set<OutboxMessage>().AsNoTracking();

            var total = await q.LongCountAsync(ct);

            var processed = await q
                .Where(x => x.ProcessedAtUtc != null)
                .LongCountAsync(ct);

            var pending = await q
                .Where(x => x.ProcessedAtUtc == null && x.AttemptCount == 0)
                .LongCountAsync(ct);

            var failed = await q
                .Where(x => x.ProcessedAtUtc == null && x.AttemptCount > 0)
                .LongCountAsync(ct);

            // Locked columns are optional across versions; avoid EF.Property when not in the model.
            var entityType = db.Model.FindEntityType(typeof(OutboxMessage));
            var hasLockedUntil = entityType?.FindProperty("LockedUntilUtc") is not null;

            long locked = 0;
            if (hasLockedUntil)
            {
                locked = await q
                    .Where(x =>
                        x.ProcessedAtUtc == null
                        && EF.Property<DateTime?>(x, "LockedUntilUtc") != null
                        && EF.Property<DateTime?>(x, "LockedUntilUtc")! > now)
                    .LongCountAsync(ct);
            }

            var oldestPending = await q
                .Where(x => x.ProcessedAtUtc == null)
                .OrderBy(x => x.OccurredAtUtc)
                .Select(x => (DateTime?)x.OccurredAtUtc)
                .FirstOrDefaultAsync(ct);

            var oldestFailed = await q
                .Where(x => x.ProcessedAtUtc == null && x.AttemptCount > 0)
                .OrderBy(x => x.OccurredAtUtc)
                .Select(x => (DateTime?)x.OccurredAtUtc)
                .FirstOrDefaultAsync(ct);

            return new OutboxStatsDto(
                Total: total,
                Pending: pending,
                Failed: failed,
                Processed: processed,
                Locked: locked,
                OldestPendingOccurredAtUtc: oldestPending,
                OldestFailedOccurredAtUtc: oldestFailed);
        }

        public async Task<bool> ReplayAsync(Guid id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var key = new OutboxMessageId(id);

            var msg = await db.Set<OutboxMessage>()
                .SingleOrDefaultAsync(x => x.Id == key, ct);

            if (msg is null)
                return false;

            var entry = db.Entry(msg);

            entry.Property(nameof(OutboxMessage.ProcessedAtUtc)).CurrentValue = null;
            entry.Property(nameof(OutboxMessage.AttemptCount)).CurrentValue = 0;
            entry.Property(nameof(OutboxMessage.LastError)).CurrentValue = null;

            ResetIfPresent(entry, "LockedUntilUtc", null);
            ResetIfPresent(entry, "LockedOwner", null);

            ResetIfPresent(entry, "DeadLetteredAtUtc", null);
            ResetIfPresent(entry, "DeadLetterReason", null);

            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var key = new OutboxMessageId(id);

            var affected = await db.Set<OutboxMessage>()
                .Where(x => x.Id == key)
                .ExecuteDeleteAsync(ct);

            return affected > 0;
        }

        private static IQueryable<OutboxMessage> ApplyFilters(
            IQueryable<OutboxMessage> q,
            DateTime? fromUtc,
            DateTime? toUtc,
            string? type,
            OutboxMessageState state)
        {
            if (fromUtc is not null)
                q = q.Where(x => x.OccurredAtUtc >= fromUtc.Value);

            if (toUtc is not null)
                q = q.Where(x => x.OccurredAtUtc <= toUtc.Value);

            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(x => EF.Functions.ILike(x.Type, $"%{type}%"));

            q = state switch
            {
                OutboxMessageState.Pending => q.Where(x => x.ProcessedAtUtc == null && x.AttemptCount == 0),
                OutboxMessageState.Failed => q.Where(x => x.ProcessedAtUtc == null && x.AttemptCount > 0),
                OutboxMessageState.Processed => q.Where(x => x.ProcessedAtUtc != null),
                _ => q
            };

            return q;
        }

        private static IQueryable<OutboxMessage> ApplySort(IQueryable<OutboxMessage> q, Sort sort)
        {
            var by = (sort.By ?? "occurredAtUtc").Trim().ToLowerInvariant();
            var desc = sort.Direction == SortDirection.Desc;

            return by switch
            {
                "attemptcount" => desc ? q.OrderByDescending(x => x.AttemptCount) : q.OrderBy(x => x.AttemptCount),
                "processedatutc" => desc ? q.OrderByDescending(x => x.ProcessedAtUtc) : q.OrderBy(x => x.ProcessedAtUtc),
                "type" => desc ? q.OrderByDescending(x => x.Type) : q.OrderBy(x => x.Type),
                _ => desc ? q.OrderByDescending(x => x.OccurredAtUtc) : q.OrderBy(x => x.OccurredAtUtc),
            };
        }

        private static void ResetIfPresent(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, object? value)
        {
            var prop = entry.Metadata.FindProperty(propertyName);
            if (prop is null)
                return;

            entry.Property(propertyName).CurrentValue = value;
        }
    }
}
