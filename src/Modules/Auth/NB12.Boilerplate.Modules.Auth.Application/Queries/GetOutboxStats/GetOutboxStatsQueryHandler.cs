using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxStats
{
    internal sealed class GetOutboxStatsQueryHandler
        : IRequestHandler<GetOutboxStatsQuery, Result<OutboxStatsDto>>
    {
        private readonly IOutboxAdminRepository _repo;

        public GetOutboxStatsQueryHandler(IOutboxAdminRepository repo) => _repo = repo;

        public async Task<Result<OutboxStatsDto>> Handle(GetOutboxStatsQuery q, CancellationToken ct)
        {
            var stats = await _repo.GetStatsAsync(ct);
            return Result<OutboxStatsDto>.Success(stats);
        }
    }
}
