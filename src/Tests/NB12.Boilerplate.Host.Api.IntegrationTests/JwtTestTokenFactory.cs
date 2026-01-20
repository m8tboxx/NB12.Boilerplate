using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public static class JwtTestTokenFactory
    {
        public static string CreateToken(
            IConfiguration config,
            string userId,
            string email,
            string securityStamp,
            IEnumerable<string> roles,
            IEnumerable<string> permissions)
        {
            var issuer = config["Auth:Jwt:Issuer"]!;
            var audience = config["Auth:Jwt:Audience"]!;
            var signingKey = config["Auth:Jwt:SigningKey"]!;

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),

            // WICHTIG: SecurityStampValidator erwartet exakt "sst"
            new("sst", securityStamp),
        };

            foreach (var r in roles.Distinct(StringComparer.OrdinalIgnoreCase))
                claims.Add(new Claim(ClaimTypes.Role, r));

            foreach (var p in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
                claims.Add(new Claim("permission", p));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
