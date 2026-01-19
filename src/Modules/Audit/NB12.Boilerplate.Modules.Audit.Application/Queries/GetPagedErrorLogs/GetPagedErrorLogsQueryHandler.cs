using MediatR;
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
            var page = q.Page < 1 ? 1 : q.Page;
            var size = q.PageSize is < 1 or > 500 ? 50 : q.PageSize;

            var result = await _repo.GetErrorLogsAsync(
                q.FromUtc, q.ToUtc, q.UserId, q.TraceId,
                page, size, ct);

            return Result<PagedResponse<ErrorLogDto>>.Success(result);
        }
    }
}
