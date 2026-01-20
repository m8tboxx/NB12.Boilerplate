using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetUserRole
{
    public sealed record GetUserRolesQuery(string UserId)
        : IRequest<IReadOnlyList<string>>;
}
