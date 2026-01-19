using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Security;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auth
{
    /// <summary>
    /// Builds authorization policies on-demand for permission-like policy names.
    /// This avoids having to register every permission as a static policy at startup.
    /// </summary>
    public sealed class DynamicPermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Heuristic: permission keys are typically dot-separated (e.g. "auth.roles.read").
            if (LooksLikePermission(policyName))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return base.GetPolicyAsync(policyName);
        }

        private static bool LooksLikePermission(string policyName)
            => policyName.Contains('.', StringComparison.Ordinal);
    }
}
