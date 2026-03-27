namespace Content.Application.Models.QueryModels
{
    public sealed class ArticleRevisionListQuery
    {
        public long ArticleId { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }
    }
}