namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Inbox
{
    public sealed record InboxCleanupOptions
    {
        public bool Enabled { get; init; } = false;

        /// <summary>Run interval for cleanup.</summary>
        public int RunEveryMinutes { get; init; } = 60;

        /// <summary>Retention for processed rows.</summary>
        public int RetainProcessedDays { get; init; } = 30;

        /// <summary>
        /// Retention for failed/unprocessed rows. If 0, failed rows are never deleted.
        /// </summary>
        public int RetainFailedDays { get; init; } = 0;
    }
}
