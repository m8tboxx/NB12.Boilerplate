using NB12.Boilerplate.Modules.Auth.Domain.Entities;

namespace NB12.Boilerplate.Modules.Auth.Application.Abstractions
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct);
        Task AddAsync(UserProfile profile, CancellationToken ct);
        void Update(UserProfile profile);
    }
}
