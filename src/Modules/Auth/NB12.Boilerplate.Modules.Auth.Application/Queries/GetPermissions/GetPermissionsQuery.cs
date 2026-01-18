using MediatR;
using NB12.Boilerplate.Modules.Auth.Application.Dtos;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetPermissions
{
    public sealed record GetPermissionsQuery()
        : IRequest<IReadOnlyList<PermissionDto>>;
}
