using System.ComponentModel.DataAnnotations;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence.Seeding
{
    public sealed class AuthSeedingOptions
    {
        public const string SectionName = "Auth:Seeding";

        public bool EnableMigrations { get; init; } = false;

        public SystemAdminOptions SystemAdmin { get; init; } = new();

        public sealed class SystemAdminOptions
        {
            public bool Enabled { get; init; } = false;

            [Required]
            public string UserName { get; init; } = "Godfather";

            [Required, EmailAddress]
            public string Email { get; init; } = "office@nb12-concepts.com";

            [Required, MinLength(12)]
            public string Password { get; init; } = "ChangeMe2026!";

            public string RoleName { get; init; } = "Admin";
        }
    }
}
