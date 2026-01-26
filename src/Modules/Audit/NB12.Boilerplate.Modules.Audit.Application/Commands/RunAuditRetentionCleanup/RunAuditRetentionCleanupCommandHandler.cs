using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Commands.RunAuditRetentionCleanup
{
    internal sealed class RunAuditRetentionCleanupCommandHandler
        : IRequestHandler<RunAuditRetentionCleanupCommand, Result<AuditRetentionCleanupResultDto>>
    {
        private readonly IAuditRetentionService _svc;

        public RunAuditRetentionCleanupCommandHandler(IAuditRetentionService svc) => _svc = svc;

        public async Task<Result<AuditRetentionCleanupResultDto>> Handle(RunAuditRetentionCleanupCommand cmd, CancellationToken ct)
        {
            var res = await _svc.RunCleanupAsync(DateTime.UtcNow, ct);
            return Result<AuditRetentionCleanupResultDto>.Success(res);
        }
    }
}
