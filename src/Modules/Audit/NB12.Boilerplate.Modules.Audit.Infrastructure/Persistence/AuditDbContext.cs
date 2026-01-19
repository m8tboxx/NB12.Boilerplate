using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence
{
    public sealed class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.HasDefaultSchema("audit");

            b.Entity<AuditLog>(e =>
            {
                e.ToTable("AuditLogs");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new AuditLogId(value))
                .ValueGeneratedNever();

                e.Property(x => x.OccurredAtUtc).IsRequired();
                e.Property(x => x.EntityType).IsRequired();
                e.Property(x => x.EntityId).IsRequired();
                e.Property(x => x.Operation).IsRequired();
                e.Property(x => x.ChangesJson).HasColumnType("jsonb").IsRequired();
                e.Property(x => x.TraceId);
                e.Property(x => x.CorrelationId);
                e.Property(x => x.UserId);
                e.Property(x => x.Email);

                e.HasIndex(x => x.OccurredAtUtc);
                e.HasIndex(x => new { x.EntityType, x.EntityId });
            });

            b.Entity<ErrorLog>(e =>
            {
                e.ToTable("ErrorLogs");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new ErrorLogId(value))
                .ValueGeneratedNever();

                e.Property(x => x.Message).IsRequired();

                e.HasIndex(x => x.OccurredAtUtc);
                e.HasIndex(x => x.TraceId);
            });
        }
    }
}
