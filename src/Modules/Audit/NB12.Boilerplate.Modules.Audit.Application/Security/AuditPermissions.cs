using NB12.Boilerplate.BuildingBlocks.Application.Security;

namespace NB12.Boilerplate.Modules.Audit.Application.Security
{
    public static class AuditPermissions
    {
        public const string AuditLogsRead = "audit.logs.read";
        public const string ErrorLogsRead = "audit.errors.read";

        // Inbox Admin
        public const string InboxRead = "audit.inbox.read";
        public const string InboxManage = "audit.inbox.manage";

        // Retention Admin
        public const string RetentionRead = "audit.retention.read";
        public const string RetentionRun = "audit.retention.run";

        public static IReadOnlyList<PermissionDefinition> All { get; } =
        [
            new(AuditLogsRead, "Read audit logs", "Read entity audit trail entries.", "Audit"),
            new(ErrorLogsRead, "Read error logs", "Read persisted error logs.", "Audit"),

            new(InboxRead, "Read inbox", "Read inbox messages (deduplication/processing state).", "Audit"),
            new(InboxManage, "Manage inbox", "Cleanup/reset inbox messages.", "Audit"),

            new(RetentionRead, "Read retention config", "Read audit log retention settings.", "Audit"),
            new(RetentionRun, "Run retention cleanup", "Run audit log retention cleanup.", "Audit"),
        ];
    }
}
