using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Auth.Application.Abstractions;
using NB12.Boilerplate.Modules.Auth.Application.Enums;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;
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

        public void Update(RefreshToken token)
            => _authDbContext.RefreshTokens.Update(token);

        public async Task<RefreshTokenRotationResult> RotateAsync(
            string currentTokenHash,
            string newTokenHash,
            DateTimeOffset newExpiresAt,
            CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            // 1) Minimaldaten lesen (NoTracking)
            var info = await _authDbContext.RefreshTokens
                .AsNoTracking()
                .Where(x => x.TokenHash == currentTokenHash)
                .Select(x => new
                {
                    x.UserId,
                    x.FamilyId,
                    x.ExpiresAt,
                    x.RevokedAt
                })
                .SingleOrDefaultAsync(ct);

            if (info is null || now >= info.ExpiresAt)
                return new RefreshTokenRotationResult(RefreshTokenRotationStatus.InvalidOrExpired, null, null);

            if (info.RevokedAt is not null)
                return new RefreshTokenRotationResult(RefreshTokenRotationStatus.ReuseDetected, info.UserId, info.FamilyId);

            await using var tx = await _authDbContext.Database.BeginTransactionAsync(ct);

            // 2) Atomar revoken (nur wenn noch aktiv)
            var affected = await _authDbContext.RefreshTokens
                .Where(x => x.TokenHash == currentTokenHash && x.RevokedAt == null && x.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, "Rotated")
                    .SetProperty(x => x.ReplacedByTokenHash, newTokenHash),
                    ct);

            if (affected == 0)
            {
                await tx.RollbackAsync(ct);
                return new RefreshTokenRotationResult(RefreshTokenRotationStatus.ReuseDetected, info.UserId, info.FamilyId);
            }

            // 3) Neuen Token anlegen (gleiche FamilyId)
            _authDbContext.RefreshTokens.Add(new RefreshToken(
                RefreshTokenId.New(),
                info.UserId,
                info.FamilyId,
                newTokenHash,
                newExpiresAt));

            await _authDbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new RefreshTokenRotationResult(RefreshTokenRotationStatus.Rotated, info.UserId, info.FamilyId);
        }

        public async Task RevokeAllForUserAsync(string userId, string reason, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            await _authDbContext.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, reason),
                    ct);
        }

        public async Task RevokeAllForFamilyAsync(Guid familyId, string reason, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            await _authDbContext.RefreshTokens
                .Where(x => x.FamilyId == familyId && x.RevokedAt == null && x.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, reason),
                    ct);
        }
    }
}
