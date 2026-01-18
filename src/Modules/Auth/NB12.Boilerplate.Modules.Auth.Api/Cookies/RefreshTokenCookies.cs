using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.Modules.Auth.Application.Options;

namespace NB12.Boilerplate.Modules.Auth.Api.Cookies
{
    public sealed class RefreshTokenCookies
    {
        private readonly RefreshTokenOptions _options;

        public RefreshTokenCookies(IOptions<RefreshTokenOptions> options)
        {
            _options = options.Value;
        }

        public void Set(HttpResponse response, string refreshToken)
        {
            response.Cookies.Append(_options.CookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth",
                Expires = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays)
            });
        }

        public string? Get(HttpRequest request)
            => request.Cookies.TryGetValue(_options.CookieName, out var v) ? v : null;

        public void Clear(HttpResponse response)
        {
            response.Cookies.Delete(_options.CookieName, new CookieOptions
            {
                Path = "/api/auth",
                Secure = true,
                SameSite = SameSiteMode.Strict,
                HttpOnly = true
            });
        }
    }
}
