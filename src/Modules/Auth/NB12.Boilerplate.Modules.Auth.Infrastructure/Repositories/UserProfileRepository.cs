using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Repositories
{
    internal sealed class UserProfileRepository : IUserProfileRepository
    {
        private readonly AuthDbContext _authDbContext;

        public UserProfileRepository(AuthDbContext authDbContext)
        {
            _authDbContext = authDbContext;
        }

        public Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct)
            => _authDbContext.UserProfiles.SingleOrDefaultAsync(x => x.IdentityUserId == userId, ct);

        public Task AddAsync(UserProfile profile, CancellationToken ct)
            => _authDbContext.UserProfiles.AddAsync(profile, ct).AsTask();

        public void Update(UserProfile profile) => _authDbContext.UserProfiles.Update(profile);
    }
}
