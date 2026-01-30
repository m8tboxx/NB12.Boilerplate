using Microsoft.EntityFrameworkCore;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public static class OutboxModelBuilderExtensions
    {
        public static void AddOutbox(this ModelBuilder modelBuilder, string tableName = "OutboxMessages")
        {
            modelBuilder.Entity<OutboxMessage>(builder =>
            {
                builder.ToTable(tableName);
                builder.HasKey(x => x.Id);

                builder.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new Ids.OutboxMessageId(value))
                .ValueGeneratedNever();

                builder.Property(x => x.Type).IsRequired();
                builder.Property(x => x.Content).IsRequired();
                builder.Property(x => x.OccurredAtUtc).IsRequired();

                builder.Property(x => x.ProcessedAtUtc);
                builder.Property(x => x.AttemptCount).IsRequired();
                builder.Property(x => x.LastError);

                builder.Property(x => x.LockedUntilUtc);
                builder.Property(x => x.LockedBy);

                builder.Property(x => x.DeadLetteredAtUtc);
                builder.Property(x => x.DeadLetterReason);

                builder.HasIndex(x => x.ProcessedAtUtc);
                builder.HasIndex(x => x.LockedUntilUtc);
                builder.HasIndex(x => x.DeadLetteredAtUtc);
                builder.HasIndex(x => x.OccurredAtUtc);

                builder.HasIndex(x => new { x.ProcessedAtUtc, x.DeadLetteredAtUtc, x.LockedUntilUtc, x.OccurredAtUtc });
            });
        }
    }
}
