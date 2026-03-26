namespace CommercialNews.BuildingBlocks.Contracts.Common
{
    public sealed class PagedResponse<T>
    {
        public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

        public PageInfo PageInfo { get; init; } = new();
    }
}

