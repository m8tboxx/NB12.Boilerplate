using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.Logout
{
    internal sealed class LogoutEverywhereCommandHandler : IRequestHandler<LogoutEverywhereCommand, Result>
    {
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IUnitOfWork _uow;
        private readonly IIdentityService _identity;

        public LogoutEverywhereCommandHandler(IRefreshTokenRepository refreshRepo, IUnitOfWork uow, IIdentityService identity)
        {
            _refreshRepo = refreshRepo;
            _uow = uow;
            _identity = identity;
        }

        public async Task<Result> Handle(LogoutEverywhereCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Result.Fail(Error.Unauthorized("auth.not_authenticated", "Not authenticated"));

            await _refreshRepo.RevokeAllForUserAsync(request.UserId, "Logout everywhere", ct);
            await _identity.InvalidateUserTokensAsync(request.UserId, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
