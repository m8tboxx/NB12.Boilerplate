using Microsoft.AspNetCore.Authorization;
using NB12.Boilerplate.BuildingBlocks.Application.Security;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auth
{
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User.HasClaim(PermissionClaimTypes.Permission, requirement.Permission))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
