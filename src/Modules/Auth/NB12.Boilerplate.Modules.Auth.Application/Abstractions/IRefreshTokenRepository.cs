using NB12.Boilerplate.Modules.Auth.Domain.Entities;

namespace NB12.Boilerplate.Modules.Auth.Application.Abstractions
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
        Task AddAsync(RefreshToken token, CancellationToken ct);
        void Update(RefreshToken token);
        Task RevokeAllForUserAsync(string userId, string reason, CancellationToken ct);
        Task RevokeAllForFamilyAsync(Guid familyId, string reason, CancellationToken ct);
        Task<RefreshTokenRotationResult> RotateAsync(
            string currentTokenHash,
            string newTokenHash,
            DateTimeOffset newExpiresAt,
            CancellationToken ct);
    }
}
