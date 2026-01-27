using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence
{
    public sealed class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
        internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

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

                e.Property(x => x.IntegrationEventId).IsRequired();
                e.Property(x => x.OccurredAtUtc).IsRequired();
                e.Property(x => x.Module).IsRequired();
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
                e.HasIndex(x => x.IntegrationEventId);
                e.HasIndex(x => new { x.Module, x.EntityType, x.EntityId });
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

            b.Entity<InboxMessage>(e =>
            {
                e.ToTable("InboxMessages");

                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new InboxMessageId(value))
                .ValueGeneratedNever();

                //e.HasKey(x => new { x.IntegrationEventId, x.HandlerName });

                e.Property(x => x.IntegrationEventId).IsRequired();
                e.Property(x => x.HandlerName).IsRequired();
                e.Property(x => x.EventType).IsRequired();
                e.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();

                e.Property(x => x.ReceivedAtUtc).IsRequired();
                e.Property(x => x.ProcessedAtUtc);

                e.Property(x => x.AttemptCount).IsRequired();
                e.Property(x => x.LastError);

                e.Property(x => x.LockedUntilUtc);
                e.Property(x => x.LockedOwner);
                
                e.Property(x => x.DeadLetteredAtUtc);
                e.Property(x => x.DeadLetterReason);

                e.HasIndex(x => new { x.IntegrationEventId, x.HandlerName }).IsUnique();

                e.HasIndex(x => x.ReceivedAtUtc);
                e.HasIndex(x => x.ProcessedAtUtc);
                e.HasIndex(x => x.AttemptCount);
                e.HasIndex(x => x.IntegrationEventId);
                e.HasIndex(x => x.HandlerName);
                e.HasIndex(x => x.LockedUntilUtc);
            });
        }
    }
}
