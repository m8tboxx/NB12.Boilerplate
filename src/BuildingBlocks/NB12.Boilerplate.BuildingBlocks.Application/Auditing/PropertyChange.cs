namespace NB12.Boilerplate.BuildingBlocks.Application.Auditing
{
    public sealed record PropertyChange(
        string Property,
        string? OldValue,
        string? NewValue);
}
