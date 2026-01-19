using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetPagedAuditLogs
{
    internal sealed class GetPagedAuditLogsQueryHandler
        : IRequestHandler<GetPagedAuditLogsQuery, Result<PagedResponse<AuditLogDto>>>
    {
        private readonly IAuditReadRepository _repo;

        public GetPagedAuditLogsQueryHandler(IAuditReadRepository repo) => _repo = repo;

        public async Task<Result<PagedResponse<AuditLogDto>>> Handle(GetPagedAuditLogsQuery q, CancellationToken ct)
        {
            var page = q.Page < 1 ? 1 : q.Page;
            var size = q.PageSize is < 1 or > 500 ? 50 : q.PageSize;

            var result = await _repo.GetAuditLogsAsync(
                q.FromUtc, q.ToUtc, q.EntityType, q.EntityId, q.Operation, q.UserId, q.TraceId,
                page, size, ct);

            return Result<PagedResponse<AuditLogDto>>.Success(result);
        }
    }
}
