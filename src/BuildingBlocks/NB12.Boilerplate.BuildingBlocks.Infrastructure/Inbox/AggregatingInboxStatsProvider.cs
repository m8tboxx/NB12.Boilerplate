using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public sealed class AggregatingInboxStatsProvider(IEnumerable<IModuleInboxStatsProvider> modules) : IInboxStatsProvider
    {
        public InboxStatsSnapshot GetSnapshot()
        {
            long total = 0, pending = 0, processed = 0, failed = 0, locked = 0;
            DateTime lastUpdated = DateTime.MinValue;

            foreach (var m in modules)
            {
                var s = m.GetSnapshot();
                total += s.Total;
                pending += s.Pending;
                processed += s.Processed;
                failed += s.Failed;
                locked += s.Locked;
                if (s.LastUpdatedUtc > lastUpdated) lastUpdated = s.LastUpdatedUtc;
            }

            return new InboxStatsSnapshot(total, pending, processed, failed, locked, lastUpdated);
        }
    }
}
