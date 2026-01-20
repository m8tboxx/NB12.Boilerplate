using MediatR;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetPagedErrorLogs
{
    internal sealed class GetPagedErrorLogsQueryHandler
        : IRequestHandler<GetPagedErrorLogsQuery, Result<PagedResponse<ErrorLogDto>>>
    {
        private readonly IAuditReadRepository _repo;

        public GetPagedErrorLogsQueryHandler(IAuditReadRepository repo) => _repo = repo;

        public async Task<Result<PagedResponse<ErrorLogDto>>> Handle(GetPagedErrorLogsQuery q, CancellationToken ct)
        {
            var page = q.Page.Normalize(defaultSize: 50, maxSize: 500);
            var sort = q.Sort;

            var result = await _repo.GetErrorLogsAsync(
                q.FromUtc, q.ToUtc, q.UserId, q.TraceId,
                page, sort, ct);

            return Result<PagedResponse<ErrorLogDto>>.Success(result);
        }
    }
}
