using System.Collections.ObjectModel;

namespace NB12.Boilerplate.Host.Shared.Ops.Dtos
{
    public sealed record OpsPagedResponse<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        long Total)
    {
        public static OpsPagedResponse<T> From(IEnumerable<T> items, int page, int pageSize, long total)
            => new(new ReadOnlyCollection<T>(items.ToList()), page, pageSize, total);
    }
}
