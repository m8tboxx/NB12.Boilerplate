using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.Logout
{
    internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokens;

        public LogoutCommandHandler(IRefreshTokenRepository refreshRepo, IUnitOfWork uow, ITokenService tokens)
        {
            _refreshRepo = refreshRepo;
            _uow = uow;
            _tokens = tokens;
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Result.Success();

            var hash = _tokens.HashRefreshToken(request.RefreshToken);
            var stored = await _refreshRepo.GetByHashAsync(hash, ct);

            if(stored is null)
                return Result.Success();

            await _refreshRepo.RevokeAllForFamilyAsync(stored.FamilyId, "Logout", ct);
            await _uow.SaveChangesAsync(ct);
            
            return Result.Success();
        }
    }
}
