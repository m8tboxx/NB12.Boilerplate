namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsHealthResponseDto(
        string Status,
        DateTime UtcNow,
        IReadOnlyList<OpsHealthCheckDto> Checks);
}
