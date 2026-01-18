using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.AddUserRole
{
    internal sealed class AddUserRoleCommandHandler : IRequestHandler<AddUserRoleCommand, Result>
    {
        private readonly IIdentityService _identity;
        public AddUserRoleCommandHandler(IIdentityService identity) => _identity = identity;

        public Task<Result> Handle(AddUserRoleCommand request, CancellationToken ct)
            => _identity.AddUserToRoleAsync(request.UserId, request.RoleName, ct);
    }
}
