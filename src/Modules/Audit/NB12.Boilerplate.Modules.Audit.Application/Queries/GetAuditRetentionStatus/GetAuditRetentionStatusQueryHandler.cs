using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetAuditRetentionStatus
{
    internal sealed class GetAuditRetentionStatusQueryHandler
        : IRequestHandler<GetAuditRetentionStatusQuery, Result<AuditRetentionStatusDto>>
    {
        private readonly IAuditRetentionStatusProvider _provider;

        public GetAuditRetentionStatusQueryHandler(IAuditRetentionStatusProvider provider)
            => _provider = provider;

        public Task<Result<AuditRetentionStatusDto>> Handle(GetAuditRetentionStatusQuery q, CancellationToken ct)
            => Task.FromResult(Result<AuditRetentionStatusDto>.Success(_provider.GetStatus()));
    }
}
