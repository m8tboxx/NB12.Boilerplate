namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsRetentionDto(
        OpsRetentionConfigDto Config,
        OpsRetentionStatusDto Status);
}
