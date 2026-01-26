namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsMetaDto(
        DateTime UtcNow,
        string? CorrelationId,
        string? TraceId);
}
