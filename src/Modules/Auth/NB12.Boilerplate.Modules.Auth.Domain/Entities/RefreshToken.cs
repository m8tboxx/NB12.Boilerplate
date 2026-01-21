using NB12.Boilerplate.BuildingBlocks.Domain.Auditing;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;

namespace NB12.Boilerplate.Modules.Auth.Domain.Entities
{
    public sealed class RefreshToken
    {
        public RefreshToken(
            RefreshTokenId id, 
            string userId,
            Guid familyId,
            string tokenHash, 
            DateTimeOffset expiresAt)
        {
            Id = id;
            UserId = userId;
            FamilyId = familyId;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public RefreshTokenId Id { get; private set; }
        public string UserId { get; private set; } = null!;
        public Guid FamilyId { get; private set; }

        [DoNotAudit]
        public string TokenHash { get; private set; } = null!;
        public string? RevokedReason { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }

        public DateTimeOffset? RevokedAt { get; private set; }
        public string? ReplacedByTokenHash { get; private set; }

        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt is not null;
        public bool IsActive => !IsExpired && !IsRevoked;

        public void Revoke(string reason, string? replacedByTokenHash = null)
        {
            if (IsRevoked)
                return;

            RevokedAt = DateTimeOffset.UtcNow;
            RevokedReason = reason;
            ReplacedByTokenHash = replacedByTokenHash;
        }
           
        public void RotateTo(string newTokenHash)
        {
            Revoke("Rotated", newTokenHash);
        }
    }
}
