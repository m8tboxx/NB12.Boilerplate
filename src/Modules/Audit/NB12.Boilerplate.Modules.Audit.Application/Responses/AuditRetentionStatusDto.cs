namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record AuditRetentionStatusDto(
        bool Enabled,
        DateTime? LastRunAtUtc,
        long? LastDeletedAuditLogs,
        long? LastDeletedErrorLogs,
        string? LastError);
}
