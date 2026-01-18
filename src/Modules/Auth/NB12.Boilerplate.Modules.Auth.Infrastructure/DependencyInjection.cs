using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Auth;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Options;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Seeding;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Repositories;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Services;
using System.Security.Claims;
using System.Text;

using JwtStampValidator = NB12.Boilerplate.Modules.Auth.Infrastructure.Security.SecurityStampValidator;


namespace NB12.Boilerplate.Modules.Auth.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AuthDb");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connectionstring 'AuthDb' is missing");

            var jwtConfig = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                ?? throw new InvalidOperationException($"Missing config section: {JwtOptions.SectionName}");

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .Validate(o => o.SigningKey.Length >= 32, "SigningKey must be at least 32 chars.")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<RefreshTokenOptions>()
                .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName);
                });
            });

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AuthDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtConfig.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtConfig.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SigningKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };

                    // HARD invalidation with SecurityStamp
                    options.Events = JwtStampValidator.CreateEvents();
                });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Permissions policies (from code catalog)
            services.AddSingleton<IPermissionCatalog, PermissionCatalog>();
            services.AddPermissionPolicies(Permissions.All);

            // CurrentUser (cross-cutting)
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();

            // Auth services
            services.AddScoped<PermissionClaimsLoader>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IRolePermissionService, RolePermissionService>();
            services.AddScoped<ITokenService, JwtTokenService>();

            // Repos/UoW
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Seeding
            services.AddScoped<AuthSeeder>();

            return services;
        }
    }
}
