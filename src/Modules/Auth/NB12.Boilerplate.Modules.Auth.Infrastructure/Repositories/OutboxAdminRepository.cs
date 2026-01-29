using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Enums;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Responses;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;
using System.Data;

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
            q = ApplySort(q, sort);

            var items = await q
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(x => new OutboxMessageDto(
                    x.Id.Value,
                    x.OccurredAtUtc,
                    x.Type,
                    x.AttemptCount,
                    x.ProcessedAtUtc,
                    x.LastError,
                    x.LockedUntilUtc,
                    x.LockedBy,
                    x.DeadLetteredAtUtc,
                    x.DeadLetterReason))
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
            q = ApplySort(q, sort);

            var items = await q
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(x => new OutboxMessageDetailsDto(
                    x.Id.Value,
                    x.OccurredAtUtc,
                    x.Type,
                    x.Content,
                    x.AttemptCount,
                    x.ProcessedAtUtc,
                    x.LastError,
                    x.LockedUntilUtc,
                    x.LockedBy,
                    x.DeadLetteredAtUtc,
                    x.DeadLetterReason))
                .ToListAsync(ct);

            return new PagedResponse<OutboxMessageDetailsDto>(items, page.Page, page.PageSize, total);
        }


        public async Task<OutboxMessageDetailsDto?> GetByIdAsync(
            Guid id, 
            CancellationToken ct)
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
                    x.LastError,
                    x.LockedUntilUtc,
                    x.LockedBy,
                    x.DeadLetteredAtUtc,
                    x.DeadLetterReason))
                .SingleOrDefaultAsync(ct);
        }

        public async Task<OutboxStatsDto> GetStatsAsync(CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var stats = await db.GetMessageStoreStatsAsync<OutboxMessage>(
                nowUtc: DateTime.UtcNow,
                timestampPropertyName: nameof(OutboxMessage.OccurredAtUtc),
                ct: ct);

            return new OutboxStatsDto(
                Total: stats.Total,
                Pending: stats.Pending,
                Failed: stats.Failed,
                Processed: stats.Processed,
                Locked: stats.Locked,
                OldestPendingOccurredAtUtc: stats.OldestPendingUtc,
                OldestFailedOccurredAtUtc: stats.OldestFailedUtc);
        }

        public async Task<OutboxAdminWriteResult> ReplayAsync(Guid id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var key = new OutboxMessageId(id);
            var msg = await db.Set<OutboxMessage>().SingleOrDefaultAsync(x => x.Id == key, ct);

            if (msg is null)
                return OutboxAdminWriteResult.NotFound;

            if (IsLocked(msg, DateTimeOffset.UtcNow))
                return OutboxAdminWriteResult.Locked;

            var entry = db.Entry(msg);

            entry.Property(nameof(OutboxMessage.ProcessedAtUtc)).CurrentValue = null;
            entry.Property(nameof(OutboxMessage.AttemptCount)).CurrentValue = 0;
            entry.Property(nameof(OutboxMessage.LastError)).CurrentValue = null;

            ResetIfPresent(entry, "LockedUntilUtc", null);
            ResetIfPresent(entry, "LockedBy", null);

            ResetIfPresent(entry, "DeadLetteredAtUtc", null);
            ResetIfPresent(entry, "DeadLetterReason", null);

            await db.SaveChangesAsync(ct);
            return OutboxAdminWriteResult.Ok;
        }


        public async Task<OutboxAdminWriteResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var key = new OutboxMessageId(id);
            var msg = await db.Set<OutboxMessage>().SingleOrDefaultAsync(x => x.Id == key, ct);

            if (msg is null)
                return OutboxAdminWriteResult.NotFound;

            if (IsLocked(msg, DateTimeOffset.UtcNow))
                return OutboxAdminWriteResult.Locked;

            db.Remove(msg);
            await db.SaveChangesAsync(ct);
            return OutboxAdminWriteResult.Ok;
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
                // Pending = unprocessed, attempt=0, NOT dead-lettered
                OutboxMessageState.Pending =>
                    q.Where(x => x.ProcessedAtUtc == null && x.AttemptCount == 0 && x.DeadLetteredAtUtc == null),

                // Failed = unprocessed, attempt>0, NOT dead-lettered
                OutboxMessageState.Failed =>
                    q.Where(x => x.ProcessedAtUtc == null && x.AttemptCount > 0 && x.DeadLetteredAtUtc == null),

                // DeadLettered = unprocessed, dead-lettered
                OutboxMessageState.DeadLettered =>
                    q.Where(x => x.ProcessedAtUtc == null && x.DeadLetteredAtUtc != null),

                OutboxMessageState.Processed =>
                    q.Where(x => x.ProcessedAtUtc != null),

                _ => q
            };

            return q;
        }

        private static IQueryable<OutboxMessage> ApplySort(
            IQueryable<OutboxMessage> q, 
            Sort sort)
        {
            var by = (sort.By ?? "occurredAtUtc").Trim().ToLowerInvariant();
            var desc = sort.Direction == SortDirection.Desc;

            return by switch
            {
                "attemptcount" => desc ? q.OrderByDescending(x => x.AttemptCount) : q.OrderBy(x => x.AttemptCount),
                "processedatutc" => desc ? q.OrderByDescending(x => x.ProcessedAtUtc) : q.OrderBy(x => x.ProcessedAtUtc),
                "type" => desc ? q.OrderByDescending(x => x.Type) : q.OrderBy(x => x.Type),
                "deadletteredatutc" => desc ? q.OrderByDescending(x => x.DeadLetteredAtUtc) : q.OrderBy(x => x.DeadLetteredAtUtc),
                "lockeduntilutc" => desc ? q.OrderByDescending(x => x.LockedUntilUtc) : q.OrderBy(x => x.LockedUntilUtc),
                "lockedby" => desc ? q.OrderByDescending(x => x.LockedBy) : q.OrderBy(x => x.LockedBy),
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

        private static bool IsLocked(OutboxMessage msg, DateTimeOffset nowUtc)
            => msg.LockedUntilUtc is not null && msg.LockedUntilUtc.Value > nowUtc;
    }
}
