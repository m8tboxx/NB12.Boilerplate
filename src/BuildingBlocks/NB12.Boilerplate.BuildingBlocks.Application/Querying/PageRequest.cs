namespace NB12.Boilerplate.BuildingBlocks.Application.Querying
{
    public readonly record struct PageRequest(int Page = 1, int PageSize = 50)
    {
        public PageRequest Normalize(int defaultSize = 50, int maxSize = 500)
        {
            var p = Page < 1 ? 1 : Page;
            var s = PageSize < 1 ? defaultSize : PageSize;
            if (s > maxSize) s = maxSize;
            return new PageRequest(p, s);
        }

        public int Skip => (Page - 1) * PageSize;
    }
}
