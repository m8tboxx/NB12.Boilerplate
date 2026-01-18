using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.RemoveRolePermissions
{
    public sealed record RemoveRolePermissionsCommand(
        string RoleId, 
        IReadOnlyList<string> Permissions) 
        : IRequest<Result>;
}
