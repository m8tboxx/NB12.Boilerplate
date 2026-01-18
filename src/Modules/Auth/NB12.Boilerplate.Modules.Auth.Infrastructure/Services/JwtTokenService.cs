using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NB12.Boilerplate.Modules.Auth.Application.Contracts;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Options;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Services
{
    internal sealed class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _jwtOptions;

        public JwtTokenService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public string CreateAccessToken(UserTokenData tokenData)
        {
            var now = DateTimeOffset.UtcNow;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, tokenData.UserId),
                new(ClaimTypes.NameIdentifier, tokenData.UserId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(SecurityStampValidator.StampClaim, tokenData.SecurityStamp ?? ""),
            };

            if (!string.IsNullOrWhiteSpace(tokenData.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, tokenData.Email));

            foreach (var role in tokenData.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
                claims.Add(new Claim(ClaimTypes.Role, role));

            foreach (var p in tokenData.Permissions.Distinct(StringComparer.OrdinalIgnoreCase))
                claims.Add(new Claim(PermissionClaimTypes.Permission, p));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: now.AddMinutes(_jwtOptions.AccessTokenMinutes).UtcDateTime,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public string HashRefreshToken(string refreshToken)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(hash);
        }
    }
}
