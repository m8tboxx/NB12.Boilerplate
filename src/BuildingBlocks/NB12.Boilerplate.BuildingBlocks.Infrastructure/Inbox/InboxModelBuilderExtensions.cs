using Microsoft.EntityFrameworkCore;
using NB12.Boilerplate.BuildingBlocks.Application.Ids;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public static class InboxModelBuilderExtensions
    {
        public static void AddInboxMessages(this ModelBuilder b, string tableName = "InboxMessages")
        {
            b.Entity<InboxMessage>(e =>
            {
                e.ToTable(tableName);

                e.HasKey(x => x.Id);
                e.Property(x => x.Id)
                .HasConversion(
                    id => id.Value,
                    value => new InboxMessageId(value))
                .ValueGeneratedNever();

                e.Property(x => x.IntegrationEventId).IsRequired();
                e.Property(x => x.HandlerName).IsRequired();

                e.Property(x => x.EventType).IsRequired();
                e.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();

                e.Property(x => x.ReceivedAtUtc).IsRequired();
                e.Property(x => x.ProcessedAtUtc);

                e.Property(x => x.AttemptCount).IsRequired();
                e.Property(x => x.LastError);
                e.Property(x => x.LastFailedAtUtc);

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
                e.HasIndex(x => x.DeadLetteredAtUtc);
            });
        }
    }
}
