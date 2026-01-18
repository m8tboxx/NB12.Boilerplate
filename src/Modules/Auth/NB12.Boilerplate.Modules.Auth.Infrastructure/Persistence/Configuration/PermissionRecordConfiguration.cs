using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Configuration
{
    internal sealed class PermissionRecordConfiguration : IEntityTypeConfiguration<PermissionRecord>
    {
        public void Configure(EntityTypeBuilder<PermissionRecord> builder)
        {
            builder.ToTable("Permissions");

            builder.HasKey(x => x.Key);
            builder.Property(x => x.Key).HasMaxLength(200);

            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Module).IsRequired().HasMaxLength(100);

            builder.Property(x => x.UpdatedAtUtc).IsRequired();
            builder.HasIndex(x => x.Module);
        }
    }
}
