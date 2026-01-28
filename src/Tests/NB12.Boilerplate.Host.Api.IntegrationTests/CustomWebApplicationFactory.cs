using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private PostgresTestDatabases? _dbs;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // 1) DBs erstellen, bevor wir Configuration/Services bauen
            var host = Environment.GetEnvironmentVariable("TEST_PG_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("TEST_PG_PORT") ?? "5432";
            var user = Environment.GetEnvironmentVariable("TEST_PG_USER") ?? "TestUser";
            var pass = Environment.GetEnvironmentVariable("TEST_PG_PASSWORD") ?? "TestMe";

            var adminConn = $"Host={host};Port={port};Username={user};Password={pass};Database=postgres";
            var baseConn = $"Host={host};Port={port};Username={user};Password={pass}";

            _dbs = new PostgresTestDatabases(adminConn, baseConn);
            _dbs.CreateAsync().GetAwaiter().GetResult();

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AuthDb"] = _dbs.AuthConnectionString,
                    ["ConnectionStrings:AuditDb"] = _dbs.AuditConnectionString,

                    // JwtOptions.SectionName = "Auth:Jwt"
                    ["Auth:Jwt:Issuer"] = "NB12",
                    ["Auth:Jwt:Audience"] = "NB12",
                    ["Auth:Jwt:SigningKey"] = "CHANGE_THIS_TO_A_LONG_RANDOM_32+_CHAR_SECRET_KEY",
                    ["Auth:Jwt:AccessTokenMinutes"] = "60",

                    // Optional: Startup-Migration/Seeding im Host deaktivieren, falls vorhanden
                    // ["Database:MigrateOnStartup"] = "false",
                    // ["Database:SeedOnStartup"] = "false",
                };

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                // 2) Migrationen nach Service-Build ausführen
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();

                var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                authDb.Database.Migrate();

                var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
                auditDb.Database.Migrate();
            });

            builder.ConfigureServices(services =>
            {
                services.PostConfigureAll<JwtBearerOptions>(o =>
                {
                    o.IncludeErrorDetails = true;

                    var original = o.Events ?? new JwtBearerEvents();

                    o.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = original.OnMessageReceived,
                        OnTokenValidated = original.OnTokenValidated,

                        OnAuthenticationFailed = ctx =>
                        {
                            // Exception-Message als Header, damit du sie im Test lesen kannst
                            ctx.Response.Headers.Append("X-Auth-Failed", ctx.Exception.Message);
                            return original.OnAuthenticationFailed?.Invoke(ctx) ?? Task.CompletedTask;
                        },

                        OnChallenge = ctx =>
                        {
                            if (!string.IsNullOrWhiteSpace(ctx.Error))
                                ctx.Response.Headers.Append("X-Auth-Error", ctx.Error);

                            if (!string.IsNullOrWhiteSpace(ctx.ErrorDescription))
                                ctx.Response.Headers.Append("X-Auth-ErrorDescription", ctx.ErrorDescription);

                            return original.OnChallenge?.Invoke(ctx) ?? Task.CompletedTask;
                        }
                    };
                });
            });

            builder.ConfigureServices(services => ConfigureTestServices(services));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            if (_dbs is not null)
            {
                _dbs.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _dbs = null;
            }
        }

        protected virtual void ConfigureTestServices(IServiceCollection services) { }
    }
}
