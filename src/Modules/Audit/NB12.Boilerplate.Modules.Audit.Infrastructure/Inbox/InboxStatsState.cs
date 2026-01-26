using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    public sealed class InboxStatsState : IInboxStatsProvider
    {
        private InboxStatsSnapshot _snapshot = new(
            Total: 0,
            Pending: 0,
            Processed: 0,
            Failed: 0,
            Locked: 0,
            LastUpdatedUtc: DateTime.MinValue);

        public InboxStatsSnapshot GetSnapshot() => _snapshot;

        public void Update(InboxStatsSnapshot snapshot) => _snapshot = snapshot;
    }
}
