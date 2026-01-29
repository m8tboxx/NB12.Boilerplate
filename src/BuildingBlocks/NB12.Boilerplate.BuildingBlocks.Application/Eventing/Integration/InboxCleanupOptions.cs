namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public sealed record InboxCleanupOptions
    {
        public bool Enabled { get; init; } = false;
        public int RunEveryMinutes { get; init; } = 60;
        public int RetainProcessedDays { get; init; } = 30;
        public int RetainFailedDays { get; init; } = 0;
    }
}
