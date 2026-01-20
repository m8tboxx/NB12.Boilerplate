using Microsoft.EntityFrameworkCore;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public sealed class EfCoreOutboxStore<TDbContext>(TDbContext db, string module) : IModuleOutboxStore
    where TDbContext : DbContext
    {
        public string Module { get; } = module;

        public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessed(int take, CancellationToken ct)
            => await db.Set<OutboxMessage>()
                .Where(x => x.ProcessedAtUtc == null)
                .OrderBy(x => x.OccurredAtUtc)
                .Take(take)
                .ToListAsync(ct);

        public async Task MarkProcessed(OutboxMessage msg, DateTime utcNow, CancellationToken ct)
        {
            msg.Proccessed(utcNow);
            await db.SaveChangesAsync(ct);
        }

        public async Task MarkFailed(OutboxMessage msg, DateTime utcNow, Exception ex, CancellationToken ct)
        {
            msg.Failed(ex.ToString());
            await db.SaveChangesAsync(ct);
        }
    }
}
