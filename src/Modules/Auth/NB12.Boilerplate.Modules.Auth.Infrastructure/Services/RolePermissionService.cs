using Microsoft.AspNetCore.Identity;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using System.Security.Claims;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Services
{
    internal sealed class RolePermissionService : IRolePermissionService
    {
        private readonly RoleManager<IdentityRole> _roles;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IPermissionCatalog _catalog;

        public RolePermissionService(RoleManager<IdentityRole> roles, UserManager<ApplicationUser> users, IPermissionCatalog catalog)
        {
            _roles = roles;
            _users = users;
            _catalog = catalog;
        }

        public async Task<Result<IReadOnlyList<string>>> GetRolePermissionsAsync(string roleId, CancellationToken ct)
        {
            var role = await _roles.FindByIdAsync(roleId);
            if (role is null)
                return Result<IReadOnlyList<string>>.Fail(Error.NotFound("auth.role.not_found", "Role not found."));

            var claims = await _roles.GetClaimsAsync(role);
            var perms = claims
                .Where(c => c.Type == PermissionClaimTypes.Permission && !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()
                .AsReadOnly();

            return Result<IReadOnlyList<string>>.Success(perms);
        }

        public async Task<Result> SetRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionKeys, CancellationToken ct)
        {
            var role = await _roles.FindByIdAsync(roleId);
            if (role is null)
                return Result.Fail(Error.NotFound("auth.role.not_found", "Role not found."));

            var valid = new HashSet<string>(_catalog.GetAll().Select(p => p.Key), StringComparer.OrdinalIgnoreCase);
            var desired = permissionKeys.Where(k => valid.Contains(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var existing = await _roles.GetClaimsAsync(role);
            var existingPermClaims = existing.Where(c => c.Type == PermissionClaimTypes.Permission).ToList();

            // remove all existing permissions
            foreach (var c in existingPermClaims)
                await _roles.RemoveClaimAsync(role, c);

            // add desired permissions
            foreach (var key in desired)
                await _roles.AddClaimAsync(role, new Claim(PermissionClaimTypes.Permission, key));

            await InvalidateUsersInRole(role.Name!);
            return Result.Success();
        }

        public async Task<Result> AddRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionKeys, CancellationToken ct)
        {
            var role = await _roles.FindByIdAsync(roleId);
            if (role is null)
                return Result.Fail(Error.NotFound("auth.role.not_found", "Role not found."));

            var valid = new HashSet<string>(_catalog.GetAll().Select(p => p.Key), StringComparer.OrdinalIgnoreCase);

            var existing = await _roles.GetClaimsAsync(role);
            var existingKeys = existing
                .Where(c => c.Type == PermissionClaimTypes.Permission)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var key in permissionKeys.Where(k => valid.Contains(k)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (existingKeys.Contains(key)) continue;
                await _roles.AddClaimAsync(role, new Claim(PermissionClaimTypes.Permission, key));
            }

            await InvalidateUsersInRole(role.Name!);
            return Result.Success();
        }

        public async Task<Result> RemoveRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionKeys, CancellationToken ct)
        {
            var role = await _roles.FindByIdAsync(roleId);
            if (role is null)
                return Result.Fail(Error.NotFound("auth.role.not_found", "Role not found."));

            var existing = await _roles.GetClaimsAsync(role);
            var toRemove = existing
                .Where(c => c.Type == PermissionClaimTypes.Permission)
                .Where(c => permissionKeys.Contains(c.Value, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var c in toRemove)
                await _roles.RemoveClaimAsync(role, c);

            await InvalidateUsersInRole(role.Name!);
            return Result.Success();
        }

        private async Task InvalidateUsersInRole(string roleName)
        {
            var users = await _users.GetUsersInRoleAsync(roleName);
            foreach (var u in users)
                await _users.UpdateSecurityStampAsync(u);
        }
    }
}
