namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsRetentionStatusDto(
        bool Enabled,
        DateTime? LastRunAtUtc,
        long? LastDeletedAuditLogs,
        long? LastDeletedErrorLogs,
        string? LastError);
}
