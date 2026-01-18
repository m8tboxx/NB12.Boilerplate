using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.RemoveRolePermissions
{
    internal sealed class RemoveRolePermissionsCommandHandler : IRequestHandler<RemoveRolePermissionsCommand, Result>
    {
        private readonly IRolePermissionService _svc;
        public RemoveRolePermissionsCommandHandler(IRolePermissionService svc) => _svc = svc;

        public Task<Result> Handle(RemoveRolePermissionsCommand request, CancellationToken ct)
            => _svc.RemoveRolePermissionsAsync(request.RoleId, request.Permissions, ct);
    }
}
