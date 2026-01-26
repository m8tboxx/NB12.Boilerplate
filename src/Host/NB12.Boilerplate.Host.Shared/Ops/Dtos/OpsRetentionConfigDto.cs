namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsRetentionConfigDto(
        bool Enabled,
        int RunEveryMinutes,
        int RetainAuditLogsDays,
        int RetainErrorLogsDays);
}
