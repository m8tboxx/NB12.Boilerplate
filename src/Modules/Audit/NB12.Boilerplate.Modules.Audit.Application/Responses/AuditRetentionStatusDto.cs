namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record AuditRetentionStatusDto(
        bool Enabled,
        DateTime? LastRunAtUtc,
        int? LastDeletedAuditLogs,
        int? LastDeletedErrorLogs,
        string? LastError);
}
