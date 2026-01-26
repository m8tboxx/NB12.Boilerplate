using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.EventBus
{
    internal sealed class NoOpInboxStatsProvider : IInboxStatsProvider
    {
        public InboxStatsSnapshot GetSnapshot()
            => new(
                Total: 0,
                Pending: 0,
                Processed: 0,
                Failed: 0,
                Locked: 0,
                LastUpdatedUtc: DateTime.MinValue);
    }
}
