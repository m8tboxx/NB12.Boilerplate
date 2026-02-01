using System.ComponentModel.DataAnnotations;

namespace NB12.Boilerplate.Modules.Audit.Application.Options
{
    public sealed class AuditRetentionOptions
    {
        public const string SectionName = "AuditRetention";

        public bool Enabled { get; set; } = true;

        [Range(1, 10080)]
        public int RunEveryMinutes { get; set; } = 60;

        [Range(1, 3650)]
        public int RetainAuditLogsDays { get; set; } = 365;

        [Range(1, 3650)]
        public int RetainErrorLogsDays { get; set; } = 180;

        // Batch-Delete für Postgres (ctid)
        [Range(100, 50_000)]
        public int BatchSize { get; set; } = 5_000;

        // Hard Cap pro Run (verhindert „endlose“ Runs)
        [Range(1_000, 1_000_000)]
        public int MaxRowsPerRun { get; set; } = 200_000;
    }
}
