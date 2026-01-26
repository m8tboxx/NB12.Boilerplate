namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsHealthCheckDto(
        string Name,
        string Status,
        string? Detail);
}
