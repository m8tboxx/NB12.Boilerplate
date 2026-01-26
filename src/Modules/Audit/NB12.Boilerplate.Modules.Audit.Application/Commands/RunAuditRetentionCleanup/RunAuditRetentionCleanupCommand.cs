using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.RunAuditRetentionCleanup
{
    public sealed record RunAuditRetentionCleanupCommand()
        : IRequest<Result<AuditRetentionCleanupResultDto>>;
}
