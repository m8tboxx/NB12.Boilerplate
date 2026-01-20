namespace NB12.Boilerplate.BuildingBlocks.Application.Querying
{
    public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total)
    {
        public long TotalPages => PageSize <= 0 ? 0 : (long)Math.Ceiling((double)Total / PageSize);
    }
}
