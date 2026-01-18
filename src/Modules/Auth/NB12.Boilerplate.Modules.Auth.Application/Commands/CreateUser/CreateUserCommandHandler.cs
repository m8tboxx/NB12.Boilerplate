using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.CreateUser
{
    internal sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<string>>
    {
        private readonly IIdentityService _identity;
        private readonly IUserProfileRepository _profiles;
        private readonly IUnitOfWork _uow;

        public CreateUserCommandHandler(IIdentityService identity, IUserProfileRepository profiles, IUnitOfWork uow)
        {
            _identity = identity;
            _profiles = profiles;
            _uow = uow;
        }

        public async Task<Result<string>> Handle(CreateUserCommand request, CancellationToken ct)
        {
            var create = await _identity.CreateUserAsync(request.Email, request.Password, ct);
            if (create.IsFailure)
                return Result<string>.Fail(create.Errors);

            var (userId, email) = create.Value;

            var profile = UserProfile.Create(
                identityUserId: userId,
                firstName: request.FirstName,
                lastName: request.LastName,
                email: email,
                locale: request.Locale,
                dateOfBirth: request.DateOfBirth,
                utcNow: DateTime.UtcNow,
                actor: "admin");

            await _profiles.AddAsync(profile, ct);

            // optional: default role
            await _identity.AddUserToRoleAsync(userId, "User", ct);

            await _uow.SaveChangesAsync(ct);
            return Result<string>.Success(userId);
        }
    }
}
