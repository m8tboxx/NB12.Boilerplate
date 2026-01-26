using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionStatus
{
    public sealed record GetAuditRetentionStatusQuery()
        : IRequest<Result<AuditRetentionStatusDto>>;
}
