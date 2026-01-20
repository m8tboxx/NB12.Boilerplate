using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.Modules.Auth.Api.Requests;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public sealed class AuthRefreshFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string RefreshCookieName = "TokenCookieNB12";

        private readonly CustomWebApplicationFactory _factory;

        public AuthRefreshFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Refresh_without_cookie_returns_401_and_problem_contains_missing_code()
        {
            var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var jar = new CookieJar();

            var resp = await client.PostEmptyWithCookiesAsync(jar, "/api/auth/refresh");

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);

            await ProblemDetailsAssert.AssertProblemAsync(
                resp,
                HttpStatusCode.Unauthorized,
                "auth.refresh_token_missing"
            );
        }

        [Fact]
        public async Task Login_refresh_logout_then_refresh_with_old_cookie_returns_401_reuse()
        {
            var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var jar = new CookieJar();

            using var scope = _factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            // Arrange: User in DB
            var (email, password) = await EnsureUserWithProfileAsync(sp);

            // 1) LOGIN
            var loginResp = await client.PostJsonWithCookiesAsync(
                jar,
                "/api/auth/login",
                new LoginRequest(email, password));

            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

            Assert.False(string.IsNullOrWhiteSpace(jar.Get(RefreshCookieName)), $"Refresh cookie '{RefreshCookieName}' was not set by /login.");

            var cookieAfterLogin = jar.Get(RefreshCookieName);
            Assert.False(string.IsNullOrWhiteSpace(cookieAfterLogin), "Refresh cookie was not set by /login.");

            var loginJson = await ReadJsonAsync(loginResp);
            var accessToken1 = loginJson.GetProperty("accessToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(accessToken1));

            // 2) REFRESH (with Cookie)
            var refreshResp = await client.PostEmptyWithCookiesAsync(jar, "/api/auth/refresh");
            Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

            var cookieAfterRefresh = jar.Get(RefreshCookieName);
            Assert.False(string.IsNullOrWhiteSpace(cookieAfterRefresh), "Refresh cookie was not set by /refresh.");
            Assert.NotEqual(cookieAfterLogin, cookieAfterRefresh);

            var refreshJson = await ReadJsonAsync(refreshResp);
            var accessToken2 = refreshJson.GetProperty("accessToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(accessToken2));
            Assert.NotEqual(accessToken1, accessToken2);

            var oldCookieAfterRefresh = cookieAfterRefresh!;

            //var loginJson = await ReadJsonAsync(loginResp);
            //var accessToken1 = loginJson.GetProperty("accessToken").GetString();
            //var refreshToken1 = loginJson.GetProperty("refreshToken").GetString();

            //Assert.False(string.IsNullOrWhiteSpace(accessToken1));
            //Assert.False(string.IsNullOrWhiteSpace(refreshToken1));

            //// 2) REFRESH (mit Cookie)
            //var refreshResp = await client.PostEmptyWithCookiesAsync(jar, "/api/auth/refresh");
            //Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

            //var refreshJson = await ReadJsonAsync(refreshResp);
            //var accessToken2 = refreshJson.GetProperty("accessToken").GetString();
            //var refreshToken2 = refreshJson.GetProperty("refreshToken").GetString();

            //Assert.False(string.IsNullOrWhiteSpace(accessToken2));
            //Assert.False(string.IsNullOrWhiteSpace(refreshToken2));
            //Assert.NotEqual(accessToken1, accessToken2);
            //Assert.NotEqual(refreshToken1, refreshToken2);

            //var oldCookieAfterRefresh = refreshToken2!; // cookie value entspricht serverseitig der neuen refreshRaw

            // 3) LOGOUT (braucht Bearer)
            var logoutResp = await client.PostEmptyWithCookiesAsync(
                jar,
                "/api/auth/logout",
                configure: req =>
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken2);
                });

            Assert.Equal(HttpStatusCode.NoContent, logoutResp.StatusCode);

            // Cookie wurde serverseitig gelöscht -> unser Jar sollte es entfernen
            Assert.True(string.IsNullOrWhiteSpace(jar.Get(RefreshCookieName)), "Refresh cookie should be cleared after /logout.");

            // 4) REFRESH mit alter Cookie-Value erzwingen -> sollte INVALID (401) sein
            // (Stärker als nur "missing cookie")
            jar.Set(RefreshCookieName, oldCookieAfterRefresh);

            var refreshAfterLogout = await client.PostEmptyWithCookiesAsync(jar, "/api/auth/refresh");
            Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);

            await ProblemDetailsAssert.AssertProblemAsync(
                refreshAfterLogout,
                HttpStatusCode.Unauthorized,
                "auth.refresh_reuse"
            );

            //var body = await refreshAfterLogout.Content.ReadAsStringAsync();
            // aus RefreshTokenCommandHandler: auth.refresh_invalid bei revoked/expired/invalid
            //Assert.True(
            //    body.Contains("auth.refresh_reuse", StringComparison.OrdinalIgnoreCase) ||
            //    body.Contains("auth.refresh_invalid", StringComparison.OrdinalIgnoreCase),
            //    $"Unexpected refresh error body: {body}"
            //);
        }

        private static async Task<(string Email, string Password)> EnsureUserWithProfileAsync(IServiceProvider sp)
        {
            var email = "it.refresh@test.local";
            var password = "TestPassword1A"; // muss deine Password Policy erfüllen

            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var db = sp.GetRequiredService<AuthDbContext>();

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var res = await userManager.CreateAsync(user, password);
                Assert.True(res.Succeeded, $"User create failed: {string.Join("; ", res.Errors.Select(e => e.Description))}");
            }

            // Dein Login braucht Profile nicht zwingend, aber du hast es im Modell verknüpft – sauber ist sauber.
            var hasProfile = await db.UserProfiles.AnyAsync(p => p.IdentityUserId == user.Id);
            if (!hasProfile)
            {
                var profile = UserProfile.Create(
                    identityUserId: user.Id,
                    firstName: "IT",
                    lastName: "Refresh",
                    email: email,
                    locale: "de-AT",
                    dateOfBirth: null,
                    utcNow: DateTime.UtcNow,
                    actor: "tests"
                );

                db.UserProfiles.Add(profile);
                await db.SaveChangesAsync();
            }

            return (email, password);
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            return doc.RootElement.Clone();
        }
    }
}
