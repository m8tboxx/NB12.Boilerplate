using Microsoft.AspNetCore.Authorization;

namespace NB12.Boilerplate.BuildingBlocks.Api.Security
{
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
            => Permission = permission;
    }
}
