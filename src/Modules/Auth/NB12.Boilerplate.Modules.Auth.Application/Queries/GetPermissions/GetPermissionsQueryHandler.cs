using MediatR;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Dtos;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetPermissions
{
    internal sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
    {
        private readonly IPermissionRepository _repo;
        public GetPermissionsQueryHandler(IPermissionRepository repo) => _repo = repo;

        public Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken ct)
            => _repo.GetAllAsync(ct);
    }
}
