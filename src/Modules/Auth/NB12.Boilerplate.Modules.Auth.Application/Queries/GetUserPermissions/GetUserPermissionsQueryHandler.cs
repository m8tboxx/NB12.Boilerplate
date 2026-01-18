using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.GetUserPermissions
{
    internal sealed class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, Result<IReadOnlyList<string>>>
    {
        private readonly IIdentityService _identity;
        public GetUserPermissionsQueryHandler(IIdentityService identity) => _identity = identity;

        public Task<Result<IReadOnlyList<string>>> Handle(GetUserPermissionsQuery request, CancellationToken ct)
            => _identity.GetUserPermissionsAsync(request.UserId, ct);
    }
}
