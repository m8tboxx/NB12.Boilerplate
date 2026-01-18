using MediatR;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetUserRole
{
    public sealed record GetUserRolesQuery(string UserId)
        : IRequest<IReadOnlyList<string>>;
}
