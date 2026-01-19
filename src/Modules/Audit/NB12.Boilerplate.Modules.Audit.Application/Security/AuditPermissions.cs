using NB12.Boilerplate.BuildingBlocks.Application.Security;

namespace NB12.Boilerplate.Modules.Audit.Application.Security
{
    public static class AuditPermissions
    {
        public const string AuditLogsRead = "audit.logs.read";
        public const string ErrorLogsRead = "audit.errors.read";

        public static IReadOnlyList<PermissionDefinition> All { get; } =
        [
            new(AuditLogsRead, "Read audit logs", "Read entity audit trail entries.", "Audit"),
            new(ErrorLogsRead, "Read error logs", "Read persisted error logs.", "Audit"),
        ];
    }
}
