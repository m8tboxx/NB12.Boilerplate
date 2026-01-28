using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public sealed class DomainEventsPostCommitRobustnessTests : IClassFixture<ThrowingDomainEventsWebApplicationFactory>
    {
        private readonly ThrowingDomainEventsWebApplicationFactory _factory;

        public DomainEventsPostCommitRobustnessTests(ThrowingDomainEventsWebApplicationFactory factory)
            => _factory = factory;

        [Fact]
        public async Task POST_create_user_succeeds_even_if_post_commit_domain_handler_throws()
        {
            var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

            using var scope = _factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var (adminId, adminEmail, adminStamp) = await EnsureUserAsync(sp, "admin@test.local", "TestPassword1A");

            var config = sp.GetRequiredService<IConfiguration>();
            var token = JwtTestTokenFactory.CreateToken(
                config,
                userId: adminId,
                email: adminEmail,
                securityStamp: adminStamp,
                roles: Array.Empty<string>(),
                permissions: new[] { AuthPermissions.Auth.UsersWrite });

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var newEmail = $"new.user.{Guid.NewGuid():N}@test.local";

            var response = await client.PostAsJsonAsync("/api/auth/admin/users", new
            {
                Email = newEmail,
                Password = "TestPassword1A",
                FirstName = "New",
                LastName = "User",
                Locale = "de-AT",
                DateOfBirth = (DateTime?)null
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Outbox muss trotzdem geschrieben worden sein (Domain->Integration Mapper läuft vor Commit)
            var db = sp.GetRequiredService<AuthDbContext>();
            var typeName = "NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents.UserCreatedIntegrationEvent";

            var hasOutbox = await db.Set<OutboxMessage>()
                .AsNoTracking()
                .AnyAsync(x => x.Type == typeName, CancellationToken.None);

            Assert.True(hasOutbox, "Expected at least one outbox message for UserCreatedIntegrationEvent.");
        }

        private static async Task<(string UserId, string Email, string SecurityStamp)> EnsureUserAsync(
            IServiceProvider sp,
            string email,
            string password)
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

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
                    throw new Xunit.Sdk.XunitException("User create failed: " + string.Join("; ", res.Errors.Select(e => e.Description)));
            }

            user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.False(string.IsNullOrWhiteSpace(user!.SecurityStamp));

            return (user.Id, email, user.SecurityStamp!);
        }
    }
}
