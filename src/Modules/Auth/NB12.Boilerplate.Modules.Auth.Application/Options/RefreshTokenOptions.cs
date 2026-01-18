using System.ComponentModel.DataAnnotations;

namespace NB12.Boilerplate.Modules.Auth.Application.Options
{
    public sealed class RefreshTokenOptions
    {
        public const string SectionName = "Auth:Refresh";

        [Range(1, 365)]
        public int RefreshTokenDays { get; set; } = 30;

        /// <summary>Cookie Name for Refresh Token (when Cookie-Flow is used).</summary>
        [Required, MinLength(1)]
        public string CookieName { get; set; } = "rt";
    }
}
