using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.MeQuery
{
    public sealed record GetMeQuery(string UserId)
    : IRequest<Result<MeResponse>>;
}
