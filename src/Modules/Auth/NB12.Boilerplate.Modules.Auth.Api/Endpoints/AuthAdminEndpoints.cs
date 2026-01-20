using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Api.ResultHandling;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.Modules.Auth.Api.Requests;
using NB12.Boilerplate.Modules.Auth.Application.Commands.AddRolePermissions;
using NB12.Boilerplate.Modules.Auth.Application.Commands.AddUserRole;
using NB12.Boilerplate.Modules.Auth.Application.Commands.CreateRole;
using NB12.Boilerplate.Modules.Auth.Application.Commands.CreateUser;
using NB12.Boilerplate.Modules.Auth.Application.Commands.DeleteRole;
using NB12.Boilerplate.Modules.Auth.Application.Commands.RemoveRolePermissions;
using NB12.Boilerplate.Modules.Auth.Application.Commands.RemoveUserRole;
using NB12.Boilerplate.Modules.Auth.Application.Commands.RenameRole;
using NB12.Boilerplate.Modules.Auth.Application.Commands.SetRolePermissions;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetPermissions;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetRolePermissions;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetRoles;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetUserPermissions;
using NB12.Boilerplate.Modules.Auth.Application.Queries.GetUserRole;
using NB12.Boilerplate.Modules.Auth.Application.Security;

namespace NB12.Boilerplate.Modules.Auth.Api.Endpoints
{
    public static class AuthAdminEndpoints
    {
        public static RouteGroupBuilder MapAuthAdminEndpoints(this RouteGroupBuilder group)
        {

            // Permissions catalog (DB)
            group.MapGet("/permissions", GetPermissions)
                .RequireAuthorization(AuthPermissions.Auth.PermissionsRead);

            // Roles
            group.MapGet("/roles", GetRoles)
                .RequireAuthorization(AuthPermissions.Auth.RolesRead);

            group.MapPost("/roles", CreateRole)
                .RequireAuthorization(AuthPermissions.Auth.RolesWrite);

            group.MapPatch("/roles/{roleId}", RenameRole)
                .RequireAuthorization(AuthPermissions.Auth.RolesWrite);

            group.MapDelete("/roles/{roleId}", DeleteRole)
                .RequireAuthorization(AuthPermissions.Auth.RolesWrite);

            // Role permissions
            group.MapGet("/roles/{roleId}/permissions", GetRolePermissions)
                .RequireAuthorization(AuthPermissions.Auth.RolesRead);

            group.MapPut("/roles/{roleId}/permissions", SetRolePermissions)
                .RequireAuthorization(AuthPermissions.Auth.RolesWrite);

            group.MapPost("/roles/{roleId}/permissions:add", AddRolePermissions)
                .RequireAuthorization(AuthPermissions.Auth.RolesWrite);

            group.MapPost("/roles/{roleId}/permissions:remove", RemoveRolePermissions)
                .RequireAuthorization(AuthPermissions.Auth.RolesWrite);

            // Users
            group.MapPost("/users", CreateUser)
                .RequireAuthorization(AuthPermissions.Auth.UsersWrite);

            group.MapGet("/users/{userId}/roles", GetUserRoles)
                .RequireAuthorization(AuthPermissions.Auth.UsersRolesRead);

            group.MapPost("/users/{userId}/roles:add", AddUserRole)
                .RequireAuthorization(AuthPermissions.Auth.UsersRolesWrite);

            group.MapPost("/users/{userId}/roles:remove", RemoveUserRole)
                .RequireAuthorization(AuthPermissions.Auth.UsersRolesWrite);

            group.MapGet("/users/{userId}/permissions", GetUserPermissions)
                .RequireAuthorization(AuthPermissions.Auth.UsersRolesRead);

            return group;
        }

        private static async Task<IResult> GetPermissions(ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new GetPermissionsQuery(), ct));

        private static async Task<IResult> GetRoles(ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new GetRolesQuery(), ct));

        private static async Task<IResult> CreateRole(CreateRoleRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new CreateRoleCommand(req.Name), ct);
            return result.ToHttpResult(http, id => Results.Ok(new { RoleId = id }));
        }

        private static async Task<IResult> RenameRole(string roleId, RenameRoleRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new RenameRoleCommand(roleId, req.NewName), ct);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> DeleteRole(string roleId, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new DeleteRoleCommand(roleId), ct);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> GetRolePermissions(string roleId, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new GetRolePermissionsQuery(roleId), ct);
            return result.ToHttpResult(http, x => Results.Ok(x));
        }

        private static async Task<IResult> SetRolePermissions(string roleId, SetRolePermissionsRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new SetRolePermissionsCommand(roleId, req.Permissions), ct);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> AddRolePermissions(string roleId, SetRolePermissionsRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new AddRolePermissionsCommand(roleId, req.Permissions), ct);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> RemoveRolePermissions(string roleId, SetRolePermissionsRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new RemoveRolePermissionsCommand(roleId, req.Permissions), ct);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> CreateUser(CreateUserRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new CreateUserCommand(req.Email, req.Password, req.FirstName, req.LastName, req.Locale, req.DateOfBirth), ct);
            return result.ToHttpResult(http, userId => Results.Ok(new { UserId = userId }));
        }

        private static async Task<IResult> GetUserRoles(string userId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new GetUserRolesQuery(userId), ct));

        private static async Task<IResult> AddUserRole(string userId, UserRoleRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new AddUserRoleCommand(userId, req.RoleName), ct);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> RemoveUserRole(string userId, UserRoleRequest req, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new RemoveUserRoleCommand(userId, req.RoleName), ct);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> GetUserPermissions(string userId, ISender sender, HttpContext http, CancellationToken ct)
        {
            var result = await sender.Send(new GetUserPermissionsQuery(userId), ct);
            return result.ToHttpResult(http, x => Results.Ok(x));
        }
    }
}
