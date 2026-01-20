using MediatR;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetPagedErrorLogs
{
    public sealed record GetPagedErrorLogsQuery(
        DateTime? FromUtc,
        DateTime? ToUtc,
        string? UserId,
        string? TraceId,
        PageRequest Page,
        Sort Sort)
        : IRequest<Result<PagedResponse<ErrorLogDto>>>;
}
