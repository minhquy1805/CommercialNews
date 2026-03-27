namespace Content.Application.Models.QueryModels
{
    public sealed class ArticleListQuery
    {
        public int Page { get; init; }
        public int PageSize { get; init; }

        public string? Status { get; init; }
        public long? CategoryId { get; init; }
        public long? TagId { get; init; }

        public string Sort { get; init; } = "-updatedAt";
    }
}

