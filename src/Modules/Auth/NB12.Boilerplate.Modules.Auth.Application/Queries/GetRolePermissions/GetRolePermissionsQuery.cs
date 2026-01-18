using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetRolePermissions
{
    public sealed record GetRolePermissionsQuery(string RoleId)
        : IRequest<Result<IReadOnlyList<string>>>;
}
