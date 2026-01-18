using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Configuration
{
    internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new RefreshTokenId(value));

            builder.Property(x => x.FamilyId)
                .IsRequired();

            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128); // sha256 hex = 64, aber Puffer ok
            builder.Property(x => x.RevokedReason).HasMaxLength(128);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();
            builder.Property(x => x.RevokedAt);
            builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);

            builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.FamilyId);
        }
    }
}
