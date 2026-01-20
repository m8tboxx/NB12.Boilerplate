using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.SetRolePermissions
{
    public sealed record SetRolePermissionsCommand(string RoleId, IReadOnlyList<string> Permissions) : IRequest<Result>;
}
