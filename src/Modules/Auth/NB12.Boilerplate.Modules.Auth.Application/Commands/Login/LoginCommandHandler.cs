using MediatR;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Options;
using NB12.Boilerplate.Modules.Auth.Application.Responses;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.Login
{
    internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly IIdentityService _identity;
        private readonly ITokenService _tokens;
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IUnitOfWork _uow;
        private readonly JwtOptions _jwt;
        private readonly RefreshTokenOptions _refresh;

        public LoginCommandHandler(
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

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
        {
            var valid = await _identity.ValidateCredentialsAsync(request.Email, request.Password, ct);
            if (valid.IsFailure)
                return Result<LoginResponse>.Fail(valid.Errors);

            var userId = valid.Value;

            var tokenDataRes = await _identity.GetUserTokenDataAsync(userId, ct);
            if (tokenDataRes.IsFailure)
                return Result<LoginResponse>.Fail(tokenDataRes.Errors);

            var accessToken = _tokens.CreateAccessToken(tokenDataRes.Value);
            var accessExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

            var familyId = Guid.NewGuid();
            var refreshRaw = _tokens.CreateRefreshToken();
            var refreshHash = _tokens.HashRefreshToken(refreshRaw);
            var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_refresh.RefreshTokenDays);

            await _refreshRepo.AddAsync(new RefreshToken(RefreshTokenId.New(), userId, familyId, refreshHash, refreshExpiresAt), ct);
            await _uow.SaveChangesAsync(ct);

            return Result<LoginResponse>.Success(new LoginResponse(accessToken, accessExpiresAt, refreshRaw));
        }
    }
}
