using Microsoft.AspNetCore.Authorization;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Security
{
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission) 
            => Permission = permission;
    }
}
