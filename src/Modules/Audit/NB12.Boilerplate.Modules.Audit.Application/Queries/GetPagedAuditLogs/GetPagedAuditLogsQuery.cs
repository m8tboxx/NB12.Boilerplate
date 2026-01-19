using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetPagedAuditLogs
{
    public sealed record GetPagedAuditLogsQuery(
        DateTime? FromUtc,
        DateTime? ToUtc,
        string? EntityType,
        string? EntityId,
        string? Operation,
        string? UserId,
        string? TraceId,
        int Page = 1,
        int PageSize = 50)
        : IRequest<Result<PagedResponse<AuditLogDto>>>;
}
