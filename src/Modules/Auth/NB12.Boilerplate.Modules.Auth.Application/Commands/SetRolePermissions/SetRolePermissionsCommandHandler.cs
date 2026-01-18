using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.SetRolePermissions
{
    internal sealed class SetRolePermissionsCommandHandler : IRequestHandler<SetRolePermissionsCommand, Result>
    {
        private readonly IRolePermissionService _svc;
        public SetRolePermissionsCommandHandler(IRolePermissionService svc) => _svc = svc;

        public Task<Result> Handle(SetRolePermissionsCommand request, CancellationToken ct)
            => _svc.SetRolePermissionsAsync(request.RoleId, request.Permissions, ct);
    }
}
