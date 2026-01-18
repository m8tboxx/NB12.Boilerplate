using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetUserPermissions
{
    public sealed record GetUserPermissionsQuery(string UserId)
        : IRequest<Result<IReadOnlyList<string>>>;
}
