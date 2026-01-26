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
        public int RetainAuditLogsDays { get; set; } = 180;

        [Range(1, 3650)]
        public int RetainErrorLogsDays { get; set; } = 180;
    }
}
