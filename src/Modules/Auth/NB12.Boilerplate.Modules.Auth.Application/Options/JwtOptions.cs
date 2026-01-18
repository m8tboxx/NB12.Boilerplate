using System.ComponentModel.DataAnnotations;

namespace NB12.Boilerplate.Modules.Auth.Application.Options
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Auth:Jwt";
        [Required]
        public required string Issuer { get; init; }
        [Required]
        public required string Audience { get; init; }

        [Required, MinLength(32)]
        public required string SigningKey { get; init; }
        [Range(1, 1440)]
        public int AccessTokenMinutes { get; init; } = 15;
    }
}
