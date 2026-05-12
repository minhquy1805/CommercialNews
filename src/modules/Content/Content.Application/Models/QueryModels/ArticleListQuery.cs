namespace Content.Application.Models.QueryModels
{
    public sealed class ArticleListQuery
    {
        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 20;

        public string? Keyword { get; init; }

        public string? Status { get; init; }

        public long? CategoryId { get; init; }

        public long? AuthorUserId { get; init; }

        public bool IsDeleted { get; init; }

        public string Sort { get; init; } = "-updatedAt";
    }
}