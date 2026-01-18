using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.CreateRole
{
    internal sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<string>>
    {
        private readonly IIdentityService _identity;
        public CreateRoleCommandHandler(IIdentityService identity) => _identity = identity;

        public Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken ct)
            => _identity.CreateRoleAsync(request.Name, ct);
    }
}
