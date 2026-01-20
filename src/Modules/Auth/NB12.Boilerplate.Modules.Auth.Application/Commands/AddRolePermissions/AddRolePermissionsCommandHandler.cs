using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.AddRolePermissions
{
    internal sealed class AddRolePermissionsCommandHandler : IRequestHandler<AddRolePermissionsCommand, Result>
    {
        private readonly IRolePermissionService _svc;
        public AddRolePermissionsCommandHandler(IRolePermissionService svc) => _svc = svc;

        public Task<Result> Handle(AddRolePermissionsCommand request, CancellationToken ct)
            => _svc.AddRolePermissionsAsync(request.RoleId, request.Permissions, ct);
    }
}
