using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetRolePermissions
{
    internal sealed class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, Result<IReadOnlyList<string>>>
    {
        private readonly IRolePermissionService _svc;
        public GetRolePermissionsQueryHandler(IRolePermissionService svc) => _svc = svc;

        public Task<Result<IReadOnlyList<string>>> Handle(GetRolePermissionsQuery request, CancellationToken ct)
            => _svc.GetRolePermissionsAsync(request.RoleId, ct);
    }
}
