namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public interface IInboxStatsProvider
    {
        InboxStatsSnapshot GetSnapshot();
    }
}
