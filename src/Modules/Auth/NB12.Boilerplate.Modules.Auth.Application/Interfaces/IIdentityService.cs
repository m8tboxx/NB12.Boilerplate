using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Contracts;

namespace NB12.Boilerplate.Modules.Auth.Application.Interfaces
{
    public interface IIdentityService
    {
        Task<Result<(string UserId, string Email)>> CreateUserAsync(string email, string password, CancellationToken ct);
        Task<Result<string>> ValidateCredentialsAsync(string email, string password, CancellationToken ct); // returns UserId
        Task<Result<(string UserId, string Email)>> GetUserAsync(string userId, CancellationToken ct);
        Task<Result<UserTokenData>> GetUserTokenDataAsync(string userId, CancellationToken ct);
        Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct);
        Task<Result> AddUserToRoleAsync(string userId, string roleName, CancellationToken ct);
        Task<Result> RemoveUserFromRoleAsync(string userId, string roleName, CancellationToken ct);
        Task<Result<string>> CreateRoleAsync(string roleName, CancellationToken ct);
        Task<Result> RenameRoleAsync(string roleId, string newName, CancellationToken ct);
        Task<Result> DeleteRoleAsync(string roleId, CancellationToken ct);
        Task<IReadOnlyList<(string Id, string Name)>> GetRolesAsync(CancellationToken ct);
        Task<Result<IReadOnlyList<string>>> GetUserPermissionsAsync(string userId, CancellationToken ct);
        Task InvalidateUserTokensAsync(string userId, CancellationToken ct);
        Task<Result> UpdateSecurityStampAsync(string userId, CancellationToken ct);
    }
}
