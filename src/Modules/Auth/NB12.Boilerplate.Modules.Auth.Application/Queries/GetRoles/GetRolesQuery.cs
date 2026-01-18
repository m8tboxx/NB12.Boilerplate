using MediatR;
using NB12.Boilerplate.Modules.Auth.Application.Dtos;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetRoles
{
    public sealed record GetRolesQuery()
        : IRequest<IReadOnlyList<RoleDto>>;
}
