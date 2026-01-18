using MediatR;
using NB12.Boilerplate.Modules.Auth.Application.Dtos;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetRoles
{
    internal sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
    {
        private readonly IIdentityService _identity;
        public GetRolesQueryHandler(IIdentityService identity) => _identity = identity;

        public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken ct)
        {
            var roles = await _identity.GetRolesAsync(ct);
            return [.. roles.Select(r => new RoleDto(r.Id, r.Name))];
        }
    }
}
