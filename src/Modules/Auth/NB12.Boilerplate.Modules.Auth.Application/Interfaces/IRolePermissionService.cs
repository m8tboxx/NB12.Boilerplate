using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Interfaces
{
    public interface IRolePermissionService
    {
        Task<Result<IReadOnlyList<string>>> GetRolePermissionsAsync(string roleId, CancellationToken ct);
        /// <summary>Replace permissions of the role (exact set).</summary>
        Task<Result> SetRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionKeys, CancellationToken ct);
        Task<Result> AddRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionKeys, CancellationToken ct);
        Task<Result> RemoveRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionKeys, CancellationToken ct);
    }
}
