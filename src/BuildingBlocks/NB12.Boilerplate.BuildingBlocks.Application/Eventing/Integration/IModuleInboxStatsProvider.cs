namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public interface IModuleInboxStatsProvider
    {
        string ModuleKey { get; }
        InboxStatsSnapshot GetSnapshot();
    }
}
