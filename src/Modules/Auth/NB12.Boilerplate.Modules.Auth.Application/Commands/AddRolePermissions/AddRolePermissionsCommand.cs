using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.AddRolePermissions
{
    public sealed record AddRolePermissionsCommand(string RoleId, IReadOnlyList<string> Permissions) 
        : IRequest<Result>;
}
