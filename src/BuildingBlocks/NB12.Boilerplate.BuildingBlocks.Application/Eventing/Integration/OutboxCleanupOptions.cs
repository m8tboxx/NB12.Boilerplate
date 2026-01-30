namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public sealed record OutboxCleanupOptions
    {
        public bool Enabled { get; init; } = false;

        public int RunEveryMinutes { get; init; } = 60;

        // Batch-Delete
        public int BatchSize { get; init; } = 5_000;

        // Retention
        public int RetainProcessedDays { get; init; } = 30;
        public int RetainDeadLetteredDays { get; init; } = 90;

        // Optional: default=0 (deactivated)
        public int RetainFailedDays { get; init; } = 0;
    }
}
