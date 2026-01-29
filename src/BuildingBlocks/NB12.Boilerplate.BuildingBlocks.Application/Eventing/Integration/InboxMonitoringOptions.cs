namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public sealed record InboxMonitoringOptions
    {
        public bool Enabled { get; init; } = false;
        public int PollSeconds { get; init; } = 30;
    }
}
