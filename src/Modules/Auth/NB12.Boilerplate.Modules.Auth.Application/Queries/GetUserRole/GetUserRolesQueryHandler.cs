using MediatR;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetUserRole
{
    internal sealed class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, IReadOnlyList<string>>
    {
        private readonly IIdentityService _identity;
        public GetUserRolesQueryHandler(IIdentityService identity) => _identity = identity;

        public Task<IReadOnlyList<string>> Handle(GetUserRolesQuery request, CancellationToken ct)
            => _identity.GetUserRolesAsync(request.UserId, ct);
    }
}
