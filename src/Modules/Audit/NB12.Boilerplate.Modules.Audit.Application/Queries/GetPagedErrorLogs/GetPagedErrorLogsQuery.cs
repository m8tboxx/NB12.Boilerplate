using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetPagedErrorLogs
{
    public sealed record GetPagedErrorLogsQuery(
        DateTime? FromUtc,
        DateTime? ToUtc,
        string? UserId,
        string? TraceId,
        int Page = 1,
        int PageSize = 50)
        : IRequest<Result<PagedResponse<ErrorLogDto>>>;
}
