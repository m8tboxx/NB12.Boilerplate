using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public sealed class AuthMeAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthMeAuthorizationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GET_auth_me_without_token_returns_401()
        {
            var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GET_auth_me_with_valid_token_but_missing_permission_returns_403()
        {
            var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

            using var scope = _factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var (userId, email, stamp) = await EnsureUserWithProfileAsync(sp);

            var config = sp.GetRequiredService<IConfiguration>();

            // intentionally without "auth.me.read"
            var token = JwtTestTokenFactory.CreateToken(
                config,
                userId: userId,
                email: email,
                securityStamp: stamp,
                roles: Array.Empty<string>(),
                permissions: new[] { "auth.roles.read" }
            );

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GET_auth_me_with_valid_token_and_permission_returns_200()
        {
            var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

            using var scope = _factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var (userId, email, stamp) = await EnsureUserWithProfileAsync(sp);

            var config = sp.GetRequiredService<IConfiguration>();

            var token = JwtTestTokenFactory.CreateToken(
                config,
                userId: userId,
                email: email,
                securityStamp: stamp,
                roles: Array.Empty<string>(),
                permissions: new[] { "auth.me.read" }
            );

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static async Task<(string UserId, string Email, string SecurityStamp)> EnsureUserWithProfileAsync(IServiceProvider sp)
        {
            var email = "it.user@test.local";
            var password = "TestPassword1A"; // must satisfy your Password policy

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
                if (!res.Succeeded)
                {
                    var msg = $"User create failed: {string.Join("; ", res.Errors.Select(e => e.Description))}";
                    throw new Xunit.Sdk.XunitException(msg);
                }
            }

            var hasProfile = await db.UserProfiles.AnyAsync(p => p.IdentityUserId == user.Id);
            if (!hasProfile)
            {
                var profile = UserProfile.Create(
                    identityUserId: user.Id,
                    firstName: "IT",
                    lastName: "User",
                    email: email,
                    locale: "de-AT",
                    dateOfBirth: null,
                    utcNow: DateTime.UtcNow,
                    actor: "tests"
                );

                db.UserProfiles.Add(profile);
                await db.SaveChangesAsync();
            }

            // SecurityStamp is required for token validation
            user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.False(string.IsNullOrWhiteSpace(user!.SecurityStamp), "SecurityStamp must not be null/empty for token validation.");

            return (user.Id, email, user.SecurityStamp!);
        }
    }
}
