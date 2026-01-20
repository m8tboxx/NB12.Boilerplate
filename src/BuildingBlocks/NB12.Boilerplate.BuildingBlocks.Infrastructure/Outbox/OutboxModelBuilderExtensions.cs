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

                builder.HasIndex(x => x.ProcessedAtUtc);
            });
        }
    }
}
