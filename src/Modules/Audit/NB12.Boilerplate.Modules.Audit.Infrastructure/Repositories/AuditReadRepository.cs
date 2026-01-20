using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Repositories
{
    internal sealed class AuditReadRepository : IAuditReadRepository
    {
        private readonly AuditDbContext _db;

        public AuditReadRepository(AuditDbContext db) => _db = db;

        public async Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? entityType,
            string? entityId,
            string? operation,
            string? userId,
            string? traceId,
            PageRequest page,
            Sort sort,
            CancellationToken ct)
        {
            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (fromUtc is not null) query = query.Where(x => x.OccurredAtUtc >= fromUtc);
            if (toUtc is not null) query = query.Where(x => x.OccurredAtUtc <= toUtc);
            if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
            if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(x => x.EntityId == entityId);
            if (!string.IsNullOrWhiteSpace(operation)) query = query.Where(x => x.Operation == operation);
            if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(x => x.UserId == userId);
            if (!string.IsNullOrWhiteSpace(traceId)) query = query.Where(x => x.TraceId == traceId);

            // Normalize paging (prevents abuse & weird values)
            var p = page.Normalize(defaultSize: 50, maxSize: 500);

            // Safe sorting (allowlist)
            query = ApplyAuditLogSort(query, sort);

            var total = await query.LongCountAsync(ct);

            var items = await query
                .Skip(p.Skip)
                .Take(p.PageSize)
                .Select(x => new AuditLogDto(
                    x.Id,
                    x.OccurredAtUtc,
                    x.UserId,
                    x.Email,
                    x.TraceId,
                    x.CorrelationId,
                    x.EntityType,
                    x.EntityId,
                    x.Operation,
                    x.ChangesJson))
                .ToListAsync(ct);

            return new PagedResponse<AuditLogDto>(items, p.Page, p.PageSize, total);
        }

        public async Task<PagedResponse<ErrorLogDto>> GetErrorLogsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? userId,
            string? traceId,
            PageRequest page, 
            Sort sort,
            CancellationToken ct)
        {
            var query = _db.ErrorLogs.AsNoTracking().AsQueryable();

            if (fromUtc is not null) query = query.Where(x => x.OccurredAtUtc >= fromUtc);
            if (toUtc is not null) query = query.Where(x => x.OccurredAtUtc <= toUtc);
            if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(x => x.UserId == userId);
            if (!string.IsNullOrWhiteSpace(traceId)) query = query.Where(x => x.TraceId == traceId);

            var p = page.Normalize(defaultSize: 50, maxSize: 500);

            query = ApplyErrorLogSort(query, sort);

            var total = await query.LongCountAsync(ct);

            var items = await query
                .Skip(p.Skip)
                .Take(p.PageSize)
                .Select(x => new ErrorLogDto(
                    x.Id,
                    x.OccurredAtUtc,
                    x.UserId,
                    x.Email,
                    x.TraceId,
                    x.CorrelationId,
                    x.Message,
                    x.ExceptionType,
                    x.StackTrace,
                    x.Path,
                    x.Method,
                    x.StatusCode))
                .ToListAsync(ct);

            return new PagedResponse<ErrorLogDto>(items, p.Page, p.PageSize, total);
        }

        private static IQueryable<AuditLog> ApplyAuditLogSort(IQueryable<AuditLog> query, Sort sort)
        {
            var by = (sort.By ?? "occurredAtUtc").Trim().ToLowerInvariant();
            var desc = sort.Direction == SortDirection.Desc;

            // Always provide deterministic tie-breaker to stabilize paging
            return by switch
            {
                "occurredatutc" or "occurredat" or "timestamp"
                    => desc
                        ? query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                "userid"
                    => desc
                        ? query.OrderByDescending(x => x.UserId).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.UserId).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                "entitytype"
                    => desc
                        ? query.OrderByDescending(x => x.EntityType).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.EntityType).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                "entityid"
                    => desc
                        ? query.OrderByDescending(x => x.EntityId).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.EntityId).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                "operation"
                    => desc
                        ? query.OrderByDescending(x => x.Operation).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.Operation).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                // default
                _
                    => query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            };
        }

        private static IQueryable<ErrorLog> ApplyErrorLogSort(IQueryable<ErrorLog> query, Sort sort)
        {
            var by = (sort.By ?? "occurredAtUtc").Trim().ToLowerInvariant();
            var desc = sort.Direction == SortDirection.Desc;

            return by switch
            {
                "occurredatutc" or "occurredat" or "timestamp"
                    => desc
                        ? query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                "userid"
                    => desc
                        ? query.OrderByDescending(x => x.UserId).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.UserId).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                "statuscode"
                    => desc
                        ? query.OrderByDescending(x => x.StatusCode).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.StatusCode).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                "exceptiontype"
                    => desc
                        ? query.OrderByDescending(x => x.ExceptionType).ThenByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                        : query.OrderBy(x => x.ExceptionType).ThenBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),

                // default
                _
                    => query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            };
        }
    }
}
