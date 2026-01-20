using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.DeleteRole
{
    internal sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
    {
        private readonly IIdentityService _identity;
        public DeleteRoleCommandHandler(IIdentityService identity) => _identity = identity;

        public Task<Result> Handle(DeleteRoleCommand request, CancellationToken ct)
            => _identity.DeleteRoleAsync(request.RoleId, ct);
    }
}
