using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.Modules.Auth.Application.Security;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Security
{
    public static class AuthPolicyRegistration
    {
        public static IServiceCollection AddPermissionPolicies(this IServiceCollection services, IEnumerable<PermissionDefinition> permissions)
        {
            services.AddAuthorization(options =>
            {
                foreach (var p in permissions)
                {
                    options.AddPolicy(p.Key, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.Requirements.Add(new PermissionRequirement(p.Key));
                    });
                }
            });

            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            return services;
        }
    }
}
