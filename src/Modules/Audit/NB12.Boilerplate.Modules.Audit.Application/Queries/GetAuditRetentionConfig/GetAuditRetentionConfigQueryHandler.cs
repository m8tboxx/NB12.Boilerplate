using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionConfig
{
    internal sealed class GetAuditRetentionConfigQueryHandler
        : IRequestHandler<GetAuditRetentionConfigQuery, Result<AuditRetentionConfigDto>>
    {
        private readonly IAuditRetentionService _svc;

        public GetAuditRetentionConfigQueryHandler(IAuditRetentionService svc) => _svc = svc;

        public Task<Result<AuditRetentionConfigDto>> Handle(GetAuditRetentionConfigQuery q, CancellationToken ct)
            => Task.FromResult(Result<AuditRetentionConfigDto>.Success(_svc.GetConfig()));
    }
}
