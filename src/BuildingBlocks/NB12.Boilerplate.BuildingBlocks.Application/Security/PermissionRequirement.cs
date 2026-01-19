using Microsoft.AspNetCore.Authorization;

namespace NB12.Boilerplate.BuildingBlocks.Application.Security
{
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
            => Permission = permission;
    }
}
