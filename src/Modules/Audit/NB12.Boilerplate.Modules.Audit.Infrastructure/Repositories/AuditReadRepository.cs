using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
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
            int page,
            int pageSize,
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

            query = query.OrderByDescending(x => x.OccurredAtUtc);

            var total = await query.LongCountAsync(ct);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditLogDto(
                    x.Id, x.OccurredAtUtc, x.UserId, x.Email, x.TraceId, x.CorrelationId,
                    x.EntityType, x.EntityId, x.Operation, x.ChangesJson))
                .ToListAsync(ct);

            return new PagedResponse<AuditLogDto>(items, page, pageSize, total);
        }

        public async Task<PagedResponse<ErrorLogDto>> GetErrorLogsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? userId,
            string? traceId,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            var query = _db.ErrorLogs.AsNoTracking().AsQueryable();

            if (fromUtc is not null) query = query.Where(x => x.OccurredAtUtc >= fromUtc);
            if (toUtc is not null) query = query.Where(x => x.OccurredAtUtc <= toUtc);
            if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(x => x.UserId == userId);
            if (!string.IsNullOrWhiteSpace(traceId)) query = query.Where(x => x.TraceId == traceId);

            query = query.OrderByDescending(x => x.OccurredAtUtc);

            var total = await query.LongCountAsync(ct);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ErrorLogDto(
                    x.Id, x.OccurredAtUtc, x.UserId, x.Email, x.TraceId, x.CorrelationId,
                    x.Message, x.ExceptionType, x.StackTrace, x.Path, x.Method, x.StatusCode))
                .ToListAsync(ct);

            return new PagedResponse<ErrorLogDto>(items, page, pageSize, total);
        }
    }
}
