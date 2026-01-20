namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox
{
    public interface IModuleOutboxStore
    {
        string Module { get; }
        Task<IReadOnlyList<OutboxMessage>> GetUnprocessed(int take, CancellationToken ct);
        Task MarkProcessed(OutboxMessage msg, DateTime utcNow, CancellationToken ct);
        Task MarkFailed(OutboxMessage msg, DateTime utcNow, Exception ex, CancellationToken ct);
    }
}
