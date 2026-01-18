using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Configuration
{
    internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder) 
        {
            builder.ToTable("UserProfiles");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new UserProfileId(value))
                .ValueGeneratedNever();

            builder.Property(x => x.IdentityUserId).IsRequired().HasMaxLength(450); // Identity key len
            builder.HasIndex(x => x.IdentityUserId).IsUnique();

            builder.Property(x => x.FirstName).IsRequired().HasMaxLength(80);
            builder.Property(x => x.LastName).IsRequired().HasMaxLength(80);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
            builder.Property(x => x.Locale).HasMaxLength(20);

            builder.Ignore(x => x.FullName);

            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.CreatedBy);
            builder.Property(x => x.LastModifiedAtUtc);
            builder.Property(x => x.LastModifiedBy);
        }
    }
}
