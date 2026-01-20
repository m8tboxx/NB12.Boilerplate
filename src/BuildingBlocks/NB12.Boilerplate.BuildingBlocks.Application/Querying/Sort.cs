namespace NB12.Boilerplate.BuildingBlocks.Application.Querying
{
    public readonly record struct Sort(string? By = null, SortDirection Direction = SortDirection.Desc);
}
