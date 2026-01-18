using NB12.Boilerplate.Modules.Auth.Application.Contracts;

namespace NB12.Boilerplate.Modules.Auth.Application.Interfaces
{
    public interface ITokenService
    {
        /// <summary>Creates Access Token (JWT) with roles, permissions and security stamp.</summary>
        string CreateAccessToken(UserTokenData tokenData);

        /// <summary>Creates Refresh Token "raw" (secret random value).</summary>
        string CreateRefreshToken();

        /// <summary>Hash of Refresh Tokens for Persistence.</summary>
        string HashRefreshToken(string refreshToken);
    }
}
