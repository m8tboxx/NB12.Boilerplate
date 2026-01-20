using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
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
            var page = q.Page.Normalize(defaultSize: 50, maxSize: 500);
            var sort = q.Sort;

            var result = await _repo.GetAuditLogsAsync(
                q.FromUtc, q.ToUtc, q.EntityType, q.EntityId, q.Operation, q.UserId, q.TraceId,
                page, sort, ct);

            return Result<PagedResponse<AuditLogDto>>.Success(result);
        }
    }
}
