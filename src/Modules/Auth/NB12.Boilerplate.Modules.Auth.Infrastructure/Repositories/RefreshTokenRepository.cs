using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Repositories
{
    internal sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AuthDbContext _authDbContext;

        public RefreshTokenRepository(AuthDbContext authDbContext)
        {
            _authDbContext = authDbContext;
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
        => _authDbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

        public Task AddAsync(RefreshToken token, CancellationToken ct)
            => _authDbContext.RefreshTokens.AddAsync(token, ct).AsTask();

        public void Update(RefreshToken token) => _authDbContext.RefreshTokens.Update(token);

        public async Task RevokeAllForUserAsync(string userId, string reason, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var tokens = await _authDbContext.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now)
                .ToListAsync(ct);

            foreach (var t in tokens)
                t.Revoke(reason);

            _authDbContext.RefreshTokens.UpdateRange(tokens);
        }

        public async Task RevokeAllForFamilyAsync(Guid familyId, string reason, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var tokens = await _authDbContext.RefreshTokens
                .Where(x => x.FamilyId == familyId && x.RevokedAt == null && x.ExpiresAt > now)
                .ToListAsync(ct);

            foreach (var t in tokens)
                t.Revoke(reason);

            _authDbContext.RefreshTokens.UpdateRange(tokens);
        }
    }
}
