using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.RenameRole
{
    internal sealed class RenameRoleCommandHandler : IRequestHandler<RenameRoleCommand, Result>
    {
        private readonly IIdentityService _identity;
        public RenameRoleCommandHandler(IIdentityService identity) => _identity = identity;

        public Task<Result> Handle(RenameRoleCommand request, CancellationToken ct)
            => _identity.RenameRoleAsync(request.RoleId, request.NewName, ct);
    }
}
