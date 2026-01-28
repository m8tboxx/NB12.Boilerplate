using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Audit.Application.Enums;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using Npgsql;
using System.Data;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Repositories
{
    internal sealed class InboxAdminRepository : IInboxAdminRepository
    {
        private readonly IDbContextFactory<AuditDbContext> _dbFactory;

        public InboxAdminRepository(IDbContextFactory<AuditDbContext> dbFactory) 
            => _dbFactory = dbFactory;

        public async Task<PagedResponse<InboxMessageDto>> GetPagedAsync(
            Guid? integrationEventId,
            string? handlerName,
            InboxMessageState state,
            DateTime? fromUtc,
            DateTime? toUtc,
            PageRequest page,
            Sort sort,
            CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            IQueryable<InboxMessage> q = db.InboxMessages.AsNoTracking();

            if (integrationEventId is not null)
                q = q.Where(x => x.IntegrationEventId == integrationEventId.Value);

            if (!string.IsNullOrWhiteSpace(handlerName))
                q = q.Where(x => EF.Functions.ILike(x.HandlerName, $"%{handlerName}%"));

            if (fromUtc is not null)
                q = q.Where(x => x.ReceivedAtUtc >= fromUtc.Value);

            if (toUtc is not null)
                q = q.Where(x => x.ReceivedAtUtc <= toUtc.Value);

            q = state switch
            {
                InboxMessageState.Pending => q.Where(x => x.ProcessedAtUtc == null && x.AttemptCount == 0 && x.DeadLetteredAtUtc == null),
                InboxMessageState.Failed => q.Where(x => x.ProcessedAtUtc == null && x.AttemptCount > 0 && x.DeadLetteredAtUtc == null),
                InboxMessageState.Processed => q.Where(x => x.ProcessedAtUtc != null),
                InboxMessageState.DeadLettered => q.Where(x => x.DeadLetteredAtUtc != null),
                _ => q
            };

            var total = await q.LongCountAsync(ct);

            q = ApplySort(q, sort);

            var items = await q
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(x => new InboxMessageDto(
                    x.Id,
                    x.IntegrationEventId,
                    x.HandlerName,
                    x.EventType,
                    x.ReceivedAtUtc,
                    x.ProcessedAtUtc,
                    x.AttemptCount,
                    x.LastError,
                    x.LockedUntilUtc,
                    x.LockedOwner,
                    x.DeadLetteredAtUtc,
                    x.DeadLetterReason))
                .ToListAsync(ct);

            return new PagedResponse<InboxMessageDto>(items, page.Page, page.PageSize, total);
        }

        public async Task<InboxMessageDetailsDto?> GetByIdAsync(InboxMessageId id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.InboxMessages
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new InboxMessageDetailsDto(
                    x.Id,
                    x.IntegrationEventId,
                    x.HandlerName,
                    x.EventType,
                    x.PayloadJson,
                    x.ReceivedAtUtc,
                    x.ProcessedAtUtc,
                    x.AttemptCount,
                    x.LastError,
                    x.LockedUntilUtc,
                    x.LockedOwner,
                    x.DeadLetteredAtUtc,
                    x.DeadLetterReason))
                .SingleOrDefaultAsync(ct);
        }

        public async Task<InboxStatsDto> GetStatsAsync(CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var stats = await db.GetMessageStoreStatsAsync<InboxMessage>(
                nowUtc: DateTime.UtcNow,
                timestampPropertyName: nameof(InboxMessage.ReceivedAtUtc),
                ct: ct);

            return new InboxStatsDto(
                Total: stats.Total,
                Pending: stats.Pending,
                Failed: stats.Failed,
                Processed: stats.Processed,
                Locked: stats.Locked,
                OldestPendingReceivedAtUtc: stats.OldestPendingUtc,
                OldestFailedReceivedAtUtc: stats.OldestFailedUtc);
        }

        public async Task<bool> ReplayAsync(InboxMessageId id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var msg = await db.InboxMessages.SingleOrDefaultAsync(x => x.Id == id, ct);

            if (msg is null)
                return false;

            msg.ResetForReplay();
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(InboxMessageId id, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var affected = await db.InboxMessages
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);

            return affected > 0;
        }

        public async Task<bool> DeleteAsync(Guid integrationEventId, string handlerName, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var sql = "DELETE FROM \"audit\".\"InboxMessages\" WHERE \"IntegrationEventId\" = @id AND \"HandlerName\" = @handler;";
            var affected = await db.Database.ExecuteSqlRawAsync(sql,
                new NpgsqlParameter("id", integrationEventId),
                new NpgsqlParameter("handler", handlerName));

            return affected > 0;
        }

        public async Task<int> CleanupProcessedBeforeAsync(DateTime beforeUtc, int maxRows, CancellationToken ct)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            // Postgres-safe limited delete via ctid
            var sql = @"
                DELETE FROM ""audit"".""InboxMessages""
                WHERE ctid IN (
                    SELECT ctid
                    FROM ""audit"".""InboxMessages""
                    WHERE ""ProcessedAtUtc"" IS NOT NULL
                      AND ""ProcessedAtUtc"" < @before
                    ORDER BY ""ProcessedAtUtc""
                    LIMIT @limit
                );";

            return await db.Database.ExecuteSqlRawAsync(sql,
                new NpgsqlParameter("before", beforeUtc),
                new NpgsqlParameter("limit", maxRows));
        }

        private static IQueryable<InboxMessage> ApplySort(IQueryable<InboxMessage> q, Sort sort)
        {
            var by = (sort.By ?? "receivedAtUtc").Trim().ToLowerInvariant();
            var desc = sort.Direction == SortDirection.Desc;

            return by switch
            {
                "attemptcount" => desc ? q.OrderByDescending(x => x.AttemptCount) : q.OrderBy(x => x.AttemptCount),
                "processedatutc" => desc ? q.OrderByDescending(x => x.ProcessedAtUtc) : q.OrderBy(x => x.ProcessedAtUtc),
                "handlername" => desc ? q.OrderByDescending(x => x.HandlerName) : q.OrderBy(x => x.HandlerName),
                "eventtype" => desc ? q.OrderByDescending(x => x.EventType) : q.OrderBy(x => x.EventType),
                _ => desc ? q.OrderByDescending(x => x.ReceivedAtUtc) : q.OrderBy(x => x.ReceivedAtUtc),
            };
        }
    }
}
