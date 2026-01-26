namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record AuditRetentionCleanupResultDto(
        DateTime RanAtUtc,
        int DeletedAuditLogs,
        int DeletedErrorLogs
    );
}
