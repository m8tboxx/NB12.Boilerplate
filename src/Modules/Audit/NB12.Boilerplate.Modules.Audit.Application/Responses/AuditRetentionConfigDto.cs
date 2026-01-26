namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record AuditRetentionConfigDto(
        bool Enabled,
        int RunEveryMinutes,
        int RetainAuditLogsDays,
        int RetainErrorLogsDays
    );
}
