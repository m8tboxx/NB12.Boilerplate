using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox
{
    public sealed class ModuleInboxStatsState : IModuleInboxStatsProvider
    {
        private InboxStatsSnapshot _snapshot = new(
            Total: 0, Pending: 0, Processed: 0, Failed: 0, Locked: 0,
            LastUpdatedUtc: DateTime.MinValue);

        public ModuleInboxStatsState(string moduleKey)
        {
            if (string.IsNullOrWhiteSpace(moduleKey))
                throw new ArgumentException("Module key must be provided.", nameof(moduleKey));
            ModuleKey = moduleKey;
        }

        public string ModuleKey { get; }

        public InboxStatsSnapshot GetSnapshot() => Volatile.Read(ref _snapshot);
        public void Update(InboxStatsSnapshot snapshot) => Volatile.Write(ref _snapshot, snapshot);
    }
}
