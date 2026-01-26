using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetOutboxStats
{
    public sealed record GetOutboxStatsQuery()
        : IRequest<Result<OutboxStatsDto>>;
}
