using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Queries.GetInboxStats
{
    internal sealed class GetInboxStatsQueryHandler
        : IRequestHandler<GetInboxStatsQuery, Result<InboxStatsDto>>
    {
        private readonly IInboxAdminRepository _repo;

        public GetInboxStatsQueryHandler(IInboxAdminRepository repo) => _repo = repo;

        public async Task<Result<InboxStatsDto>> Handle(GetInboxStatsQuery q, CancellationToken ct)
        {
            var stats = await _repo.GetStatsAsync(ct);
            return Result<InboxStatsDto>.Success(stats);
        }
    }
}
