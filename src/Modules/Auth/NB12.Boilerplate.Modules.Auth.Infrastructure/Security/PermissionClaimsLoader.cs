using Microsoft.AspNetCore.Identity;
using NB12.Boilerplate.BuildingBlocks.Application.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Security
{
    public sealed class PermissionClaimsLoader
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public PermissionClaimsLoader(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
        {
            _users = users;
            _roles = roles;
        }

        public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(ApplicationUser user)
        {
            var roleNames = await _users.GetRolesAsync(user);

            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var roleName in roleNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = await _roles.FindByNameAsync(roleName);
                if (role is null) continue;

                var claims = await _roles.GetClaimsAsync(role);
                foreach (var c in claims)
                {
                    if (c.Type == PermissionClaimTypes.Permission && !string.IsNullOrWhiteSpace(c.Value))
                        perms.Add(c.Value);
                }
            }

            return perms.OrderBy(x => x).ToList().AsReadOnly();
        }
    }
}
