using MediatR;
using NB12.Boilerplate.BuildingBlocks.Application.Querying;
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
        PageRequest Page,
        Sort Sort)
        : IRequest<Result<PagedResponse<AuditLogDto>>>;
}
