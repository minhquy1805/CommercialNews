namespace Content.Application.Models.QueryModels
{
    public sealed class PagedQueryResult<T>
    {
        public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
    }
}

