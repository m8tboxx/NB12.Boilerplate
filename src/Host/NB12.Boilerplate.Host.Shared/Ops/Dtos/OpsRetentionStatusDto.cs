namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsRetentionStatusDto(
        bool Enabled,
        DateTime? LastRunAtUtc,
        int? LastDeletedAuditLogs,
        int? LastDeletedErrorLogs,
        string? LastError);
}
