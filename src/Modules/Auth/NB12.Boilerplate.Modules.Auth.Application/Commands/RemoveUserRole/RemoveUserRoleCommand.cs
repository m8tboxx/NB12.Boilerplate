using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.RemoveUserRole
{
    public sealed record RemoveUserRoleCommand(string UserId, string RoleName) : IRequest<Result>;
}
