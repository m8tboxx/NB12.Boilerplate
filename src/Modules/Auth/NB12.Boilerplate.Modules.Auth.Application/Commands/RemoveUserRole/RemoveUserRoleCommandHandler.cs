using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.RemoveUserRole
{
    internal sealed class RemoveUserRoleCommandHandler : IRequestHandler<RemoveUserRoleCommand, Result>
    {
        private readonly IIdentityService _identity;
        public RemoveUserRoleCommandHandler(IIdentityService identity) => _identity = identity;

        public Task<Result> Handle(RemoveUserRoleCommand request, CancellationToken ct)
            => _identity.RemoveUserFromRoleAsync(request.UserId, request.RoleName, ct);
    }
}
