using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Application.Security;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence.Seeding;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using System.Security.Claims;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Seeding
{
    public sealed class AuthSeeder
    {
        private readonly AuthDbContext _db;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermissionCatalog _catalog;
        private readonly IOptions<AuthSeedingOptions> _seedingOptions;

        public AuthSeeder(AuthDbContext db, RoleManager<IdentityRole> roles, UserManager<ApplicationUser> userManager, IPermissionCatalog catalog, IOptions<AuthSeedingOptions> seedingOptions)
        {
            _db = db;
            _roles = roles;
            _userManager = userManager;
            _catalog = catalog;
            _seedingOptions = seedingOptions;
        }

        public async Task SeedAsync(CancellationToken ct = default)
        {
            var options = _seedingOptions.Value;

            if(options.EnableMigrations)
                await _db.Database.MigrateAsync(ct);

            var defs = _catalog.GetAll();
            var now = DateTime.UtcNow;

            var existing = await _db.Permissions.ToListAsync(ct);
            var existingKeys = existing.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var d in defs)
            {
                //var rec = await _db.Permissions.SingleOrDefaultAsync(x => x.Key == d.Key, ct);

                if (!existingKeys.TryGetValue(d.Key, out var rec))
                {
                    _db.Permissions.Add(new PermissionRecord
                    {
                        Key = d.Key,
                        DisplayName = d.DisplayName,
                        Description = d.Description,
                        Module = d.Module,
                        UpdatedAtUtc = now
                    });
                    continue;
                }

                rec.DisplayName = d.DisplayName;
                rec.Description = d.Description;
                rec.Module = d.Module;
                rec.UpdatedAtUtc = now;
            }

            // Remove stale keys
            var defKeys = defs.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var stale = existing.Where(p => !defKeys.Contains(p.Key)).ToList();

            if (stale.Count > 0)
                _db.Permissions.RemoveRange(stale);

            await _db.SaveChangesAsync(ct);

            // Seed default roles and permission claims
            await EnsureRoleWithPermissions("Admin", defs.Select(x => x.Key).ToList(), ct);
            await EnsureRoleWithPermissions("User", new List<string> { AuthPermissions.Auth.MeRead }, ct);

            if (options.SystemAdmin.Enabled)
                await EnsureSystemAdministratorAsync(options.SystemAdmin, ct);
        }

        private async Task EnsureRoleWithPermissions(string roleName, IReadOnlyList<string> permissions, CancellationToken ct)
        {
            var role = await _roles.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new IdentityRole(roleName);
                var create = await _roles.CreateAsync(role);
                if (!create.Succeeded)
                    throw new InvalidOperationException($"Cannot create role '{roleName}': {string.Join("; ", create.Errors.Select(e => e.Description))}");
            }

            var currentClaims = await _roles.GetClaimsAsync(role);
            var currentPerms = currentClaims
                .Where(c => c.Type == PermissionClaimTypes.Permission)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // add missing
            foreach (var p in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (currentPerms.Contains(p)) continue;
                await _roles.AddClaimAsync(role, new Claim(PermissionClaimTypes.Permission, p));
            }

            // remove extra
            var desired = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var extras = currentClaims
                .Where(c => c.Type == PermissionClaimTypes.Permission)
                .Where(c => !desired.Contains(c.Value))
                .ToList();

            foreach (var c in extras)
                await _roles.RemoveClaimAsync(role, c);
        }

        private async Task EnsureSystemAdministratorAsync(AuthSeedingOptions.SystemAdminOptions admin, CancellationToken ct)
        {
            var userName = admin.UserName;
            var email = admin.Email;
            var password = admin.Password;
            var roleName = admin.RoleName;

            // 1) User finden (erst Email, dann Username)
            var user = await _userManager.FindByEmailAsync(email)
                       ?? await _userManager.FindByNameAsync(userName);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true // Optional: in Prod sinnvoll, sonst Login-Flows ggf. blockiert
                };

                var create = await _userManager.CreateAsync(user, password);
                if (!create.Succeeded)
                    throw new InvalidOperationException(
                        $"Cannot create system administrator '{userName}': {string.Join("; ", create.Errors.Select(e => e.Description))}");
            }
            else
            {
                // Optional: Werte “gerade ziehen”, falls jemand dran gedreht hat
                var changed = false;

                if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
                {
                    user.UserName = userName;
                    changed = true;
                }

                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    user.Email = email;
                    changed = true;
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    changed = true;
                }

                if (changed)
                {
                    var update = await _userManager.UpdateAsync(user);
                    if (!update.Succeeded)
                        throw new InvalidOperationException(
                            $"Cannot update system administrator '{userName}': {string.Join("; ", update.Errors.Select(e => e.Description))}");
                }
            }

            // 2) Rolle zuweisen
            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var addToRole = await _userManager.AddToRoleAsync(user, roleName);
                if (!addToRole.Succeeded)
                    throw new InvalidOperationException(
                        $"Cannot assign role '{roleName}' to '{userName}': {string.Join("; ", addToRole.Errors.Select(e => e.Description))}");
            }

            // 3) (Optional, STRIKT): Passwort immer setzen/rotieren
            // Ich empfehle NICHT, das bei jedem Start zu tun.
            // Wenn du es einmalig erzwingen willst, kannst du nach dem Create hier stoppen.
        }

        private static async Task EnsureRoleHasPermissionsAsync(
            RoleManager<IdentityRole> roleManager,
            IdentityRole role,
            IEnumerable<string> permissionKeys)
        {
            var existing = await roleManager.GetClaimsAsync(role);
            var existingSet = existing
                .Where(c => c.Type == PermissionClaimTypes.Permission)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var key in permissionKeys)
            {
                if (existingSet.Contains(key)) continue;
                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(PermissionClaimTypes.Permission, key));
            }
        }
    }
}
