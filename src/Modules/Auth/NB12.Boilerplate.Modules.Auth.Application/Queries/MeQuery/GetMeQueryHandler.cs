using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Queries.MeQuery
{
    internal sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<MeResponse>>
    {
        private readonly IIdentityService _identity;
        private readonly IUserProfileRepository _profiles;

        public GetMeQueryHandler(IIdentityService identity, IUserProfileRepository profiles)
        {
            _identity = identity;
            _profiles = profiles;
        }

        public async Task<Result<MeResponse>> Handle(GetMeQuery request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Result<MeResponse>.Fail(Error.Unauthorized("auth.not_authenticated", "Not authenticated"));

            var profile = await _profiles.GetByUserIdAsync(request.UserId, ct);
            if (profile is null)
                return Result<MeResponse>.Fail(Error.NotFound("auth.profile_not_found", "Profile not found"));

            var tokenDataRes = await _identity.GetUserTokenDataAsync(request.UserId, ct);
            if (tokenDataRes.IsFailure)
                return Result<MeResponse>.Fail(tokenDataRes.Errors);

            var td = tokenDataRes.Value;

            return Result<MeResponse>.Success(new MeResponse(request.UserId, td.Email, profile.FullName, profile.Locale, td.Roles.ToArray()));
        }
    }
}
