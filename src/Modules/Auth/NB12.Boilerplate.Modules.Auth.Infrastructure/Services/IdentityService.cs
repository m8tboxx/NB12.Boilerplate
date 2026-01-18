using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Contracts;
using NB12.Boilerplate.Modules.Auth.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Security;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Services
{
    internal sealed class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly PermissionClaimsLoader _permissionClaimsLoader;

        public IdentityService(
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn,
            RoleManager<IdentityRole> roles,
            PermissionClaimsLoader permissionClaimsLoader)
        {
            _users = users;
            _signIn = signIn;
            _roles = roles;
            _permissionClaimsLoader = permissionClaimsLoader;
        }

        public async Task<Result<(string UserId, string Email)>> CreateUserAsync(string email, string password, CancellationToken ct)
        {
            var existing = await _users.FindByEmailAsync(email);
            if (existing is not null)
                return Result<(string, string)>.Fail(Error.Conflict("auth.email_exists", "Email already exists."));

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var res = await _users.CreateAsync(user, password);
            if (!res.Succeeded)
                return Result<(string, string)>.Fail(Error.Validation("auth.user_create_failed",
                    string.Join("; ", res.Errors.Select(e => e.Description))));

            return Result<(string, string)>.Success((user.Id, user.Email ?? email));
        }

        public async Task<Result<string>> ValidateCredentialsAsync(string email, string password, CancellationToken ct)
        {
            var user = await _users.FindByEmailAsync(email);
            if (user is null)
                return Result<string>.Fail(Error.Unauthorized("auth.invalid_credentials", "Invalid credentials."));

            var ok = await _signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (!ok.Succeeded)
                return Result<string>.Fail(Error.Unauthorized("auth.invalid_credentials", "Invalid credentials."));

            return Result<string>.Success(user.Id);
        }

        public async Task<Result<(string UserId, string Email)>> GetUserAsync(string userId, CancellationToken ct)
        {
            var user = await _users.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (user is null)
                return Result<(string, string)>.Fail(Error.NotFound("auth.user_not_found", "User not found."));

            return Result<(string, string)>.Success((user.Id, user.Email ?? ""));
        }

        public async Task<Result<UserTokenData>> GetUserTokenDataAsync(string userId, CancellationToken ct)
        {
            var user = await _users.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (user is null)
                return Result<UserTokenData>.Fail(Error.NotFound("auth.user_not_found", "User not found."));

            var roles = (await _users.GetRolesAsync(user)).OrderBy(x => x).ToList();
            var perms = await _permissionClaimsLoader.GetEffectivePermissionsAsync(user);

            return Result<UserTokenData>.Success(new UserTokenData(
                user.Id,
                user.Email ?? "",
                roles,
                perms.ToList(),
                user.SecurityStamp ?? ""));
        }

        public async Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is null) return Array.Empty<string>();
            return (await _users.GetRolesAsync(user)).OrderBy(x => x).ToList().AsReadOnly();
        }

        public async Task<Result> AddUserToRoleAsync(string userId, string roleName, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is null) return Result.Fail(Error.NotFound("auth.user_not_found", "User not found."));

            var res = await _users.AddToRoleAsync(user, roleName);
            if (!res.Succeeded)
                return Result.Fail(Error.Validation("auth.add_role_failed", string.Join("; ", res.Errors.Select(e => e.Description))));

            await _users.UpdateSecurityStampAsync(user);
            return Result.Success();
        }

        public async Task<Result> RemoveUserFromRoleAsync(string userId, string roleName, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is null) return Result.Fail(Error.NotFound("auth.user_not_found", "User not found."));

            var res = await _users.RemoveFromRoleAsync(user, roleName);
            if (!res.Succeeded)
                return Result.Fail(Error.Validation("auth.remove_role_failed", string.Join("; ", res.Errors.Select(e => e.Description))));

            await _users.UpdateSecurityStampAsync(user);
            return Result.Success();
        }

        public async Task<Result<string>> CreateRoleAsync(string roleName, CancellationToken ct)
        {
            var name = roleName.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return Result<string>.Fail(Error.Validation("auth.role.invalid", "Role name is required."));

            if (await _roles.FindByNameAsync(name) is not null)
                return Result<string>.Fail(Error.Conflict("auth.role.exists", "Role already exists."));

            var role = new IdentityRole(name);
            var res = await _roles.CreateAsync(role);

            if (!res.Succeeded)
                return Result<string>.Fail(Error.Validation("auth.role.create_failed", string.Join("; ", res.Errors.Select(e => e.Description))));

            return Result<string>.Success(role.Id);
        }

        public async Task<Result> RenameRoleAsync(string roleId, string newName, CancellationToken ct)
        {
            var role = await _roles.FindByIdAsync(roleId);
            if (role is null) return Result.Fail(Error.NotFound("auth.role.not_found", "Role not found."));

            var name = newName.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return Result.Fail(Error.Validation("auth.role.invalid", "Role name is required."));

            if (await _roles.FindByNameAsync(name) is not null)
                return Result.Fail(Error.Conflict("auth.role.exists", "Role name already exists."));

            role.Name = name;
            role.NormalizedName = name.ToUpperInvariant();

            var res = await _roles.UpdateAsync(role);
            if (!res.Succeeded)
                return Result.Fail(Error.Validation("auth.role.rename_failed", string.Join("; ", res.Errors.Select(e => e.Description))));

            // renaming role does not change permissions; no need to invalidate all users, but safe:
            var users = await _users.GetUsersInRoleAsync(role.Name);
            foreach (var u in users)
                await _users.UpdateSecurityStampAsync(u);

            return Result.Success();
        }

        public async Task<Result> DeleteRoleAsync(string roleId, CancellationToken ct)
        {
            var role = await _roles.FindByIdAsync(roleId);
            if (role is null) return Result.Fail(Error.NotFound("auth.role.not_found", "Role not found."));

            var res = await _roles.DeleteAsync(role);
            if (!res.Succeeded)
                return Result.Fail(Error.Validation("auth.role.delete_failed", string.Join("; ", res.Errors.Select(e => e.Description))));

            return Result.Success();
        }

        public async Task<IReadOnlyList<(string Id, string Name)>> GetRolesAsync(CancellationToken ct)
        {
            var list = await _roles.Roles
                .OrderBy(r => r.Name)
                .Select(r => new ValueTuple<string, string>(r.Id, r.Name!))
                .ToListAsync(ct);

            return list.AsReadOnly();
        }

        public async Task<Result<IReadOnlyList<string>>> GetUserPermissionsAsync(string userId, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is null) return Result<IReadOnlyList<string>>.Fail(Error.NotFound("auth.user_not_found", "User not found."));

            var perms = await _permissionClaimsLoader.GetEffectivePermissionsAsync(user);
            return Result<IReadOnlyList<string>>.Success(perms);
        }

        public async Task InvalidateUserTokensAsync(string userId, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is null) return;
            await _users.UpdateSecurityStampAsync(user);
        }

        public async Task<Result> UpdateSecurityStampAsync(string userId, CancellationToken ct)
        {
            // UserManager APIs haben keinen CancellationToken – ct ist trotzdem ok im Interface.
            var user = await _users.FindByIdAsync(userId);
            if (user is null)
                return Result.Fail(Error.NotFound("auth.user_not_found", "User not found."));

            await _users.UpdateSecurityStampAsync(user);
            return Result.Success();
        }
    }
}
