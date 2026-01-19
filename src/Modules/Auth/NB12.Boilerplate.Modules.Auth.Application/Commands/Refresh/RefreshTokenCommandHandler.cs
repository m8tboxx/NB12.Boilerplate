using MediatR;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Options;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

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

            // Neue Refresh Token Daten schon jetzt erzeugen (für Rotation)
            var newRefreshRaw = _tokens.CreateRefreshToken();
            var newRefreshHash = _tokens.HashRefreshToken(newRefreshRaw);
            var newRefreshExpires = DateTimeOffset.UtcNow.AddDays(_refresh.RefreshTokenDays);

            var rotation = await _refreshRepo.RotateAsync(incomingHash, newRefreshHash, newRefreshExpires, ct);

            if (rotation.Status == RefreshTokenRotationStatus.InvalidOrExpired)
                return Result<RefreshTokenResponse>.Fail(Error.Unauthorized("auth.refresh_invalid", "Invalid refresh token"));

            if (rotation.Status == RefreshTokenRotationStatus.ReuseDetected)
            {
                if (rotation.FamilyId is not null)
                    await _refreshRepo.RevokeAllForFamilyAsync(rotation.FamilyId.Value, "Refresh token reuse detected", ct);

                if (!string.IsNullOrWhiteSpace(rotation.UserId))
                    await _identity.InvalidateUserTokensAsync(rotation.UserId!, ct);

                return Result<RefreshTokenResponse>.Fail(
                    Error.Unauthorized("auth.refresh_reuse", "Refresh token reuse detected"));
            }

            // Rotated: Access Token erstellen
            var tokenDataRes = await _identity.GetUserTokenDataAsync(rotation.UserId!, ct);
            if (tokenDataRes.IsFailure)
                return Result<RefreshTokenResponse>.Fail(tokenDataRes.Errors);

            var access = _tokens.CreateAccessToken(tokenDataRes.Value);
            var accessExpires = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            return Result<RefreshTokenResponse>.Success(
                new RefreshTokenResponse(access, accessExpires, newRefreshRaw));
        }
    }
}
