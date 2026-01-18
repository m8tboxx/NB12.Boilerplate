using MediatR;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Options;
using NB12.Boilerplate.Modules.Auth.Application.Responses;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.Refresh
{
    internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
    {
        private readonly IIdentityService _identity;
        private readonly ITokenService _tokens;
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IUnitOfWork _uow;
        private readonly JwtOptions _jwt;
        private readonly RefreshTokenOptions _refresh;

        public RefreshTokenCommandHandler(
            IIdentityService identity,
            ITokenService tokens,
            IRefreshTokenRepository refreshRepo,
            IUnitOfWork uow,
            IOptions<JwtOptions> jwtOptions,
            IOptions<RefreshTokenOptions> refreshOptions)
        {
            _identity = identity;
            _tokens = tokens;
            _refreshRepo = refreshRepo;
            _uow = uow;
            _jwt = jwtOptions.Value;
            _refresh = refreshOptions.Value;
        }

        public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Result<RefreshTokenResponse>.Fail(Error.Unauthorized("auth.refresh_missing", "Missing refresh token"));

            var incomingHash = _tokens.HashRefreshToken(request.RefreshToken);
            var stored = await _refreshRepo.GetByHashAsync(incomingHash, ct);

            if (stored is null || stored.IsExpired)
                return Result<RefreshTokenResponse>.Fail(Error.Unauthorized("auth.refresh_invalid", "Invalid refresh token"));

            // Reuse / Replay detection:
            // Token existiert, ist aber bereits revoked (typisch: rotiert, logout, compromise)
            if (stored.IsRevoked)
            {
                // Kill all refresh tokens for this user (minimal but safe)
                await _refreshRepo.RevokeAllForFamilyAsync(stored.FamilyId, "Refresh token reuse detected", ct);
                
                // Optional, aber stark empfohlen: Access Tokens sofort invalidieren
                // (du prüfst 'sst' gegen SecurityStamp im JWT OnTokenValidated)
                await _identity.InvalidateUserTokensAsync(stored.UserId, ct);

                await _uow.SaveChangesAsync(ct);

                return Result<RefreshTokenResponse>.Fail(
                    Error.Unauthorized("auth.refresh_reuse", "Refresh token reuse detected"));
            }

            var tokenDataRes = await _identity.GetUserTokenDataAsync(stored.UserId, ct);
            if (tokenDataRes.IsFailure)
                return Result<RefreshTokenResponse>.Fail(tokenDataRes.Errors);

            var access = _tokens.CreateAccessToken(tokenDataRes.Value);
            var accessExpires = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            var newRefreshRaw = _tokens.CreateRefreshToken();
            var newRefreshHash = _tokens.HashRefreshToken(newRefreshRaw);
            var newRefreshExpires = DateTimeOffset.UtcNow.AddDays(_refresh.RefreshTokenDays);

            stored.RotateTo(newRefreshHash);
            _refreshRepo.Update(stored);

            await _refreshRepo.AddAsync(new RefreshToken(RefreshTokenId.New(), stored.UserId, stored.FamilyId, newRefreshHash, newRefreshExpires), ct);
            await _uow.SaveChangesAsync(ct);

            return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(access, accessExpires, newRefreshRaw));
        }
    }
}
