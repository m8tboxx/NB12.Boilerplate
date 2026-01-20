using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Interfaces
{
    public interface IAuditReadRepository
    {
        Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? entityType,
            string? entityId,
            string? operation,
            string? userId,
            string? traceId,
            PageRequest page,
            Sort sort,
            CancellationToken ct);

        Task<PagedResponse<ErrorLogDto>> GetErrorLogsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? userId,
            string? traceId,
            PageRequest page,
            Sort sort,
            CancellationToken ct);
    }
}
