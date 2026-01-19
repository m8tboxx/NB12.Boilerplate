namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
}
