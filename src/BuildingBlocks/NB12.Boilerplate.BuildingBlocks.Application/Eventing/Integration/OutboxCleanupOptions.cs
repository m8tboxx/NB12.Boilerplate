namespace NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration
{
    public sealed record OutboxCleanupOptions
    {
        public bool Enabled { get; set; } = false;

        public int RunEveryMinutes { get; set; } = 60;

        /// <summary>Batch delete size per cycle per module (avoid long locks).</summary>
        public int BatchSize { get; set; } = 5000;

        /// <summary>Delete published messages older than this.</summary>
        public int RetainPublishedDays { get; set; } = 30;

        /// <summary>Delete dead-lettered messages older than this.</summary>
        public int RetainDeadLetterDays { get; set; } = 90;

        /// <summary>Delete failed messages older than this.</summary>
        public int RetainFailedDays { get; set; } = 60;
    }
}
