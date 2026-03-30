namespace Content.Application.Models.QueryModels
{
    public sealed class TagListQuery
    {
        public int Page { get; init; }
        public int PageSize { get; init; }

        public string? Keyword { get; init; }
        public bool? IsActive { get; init; }
        public bool IsDeleted { get; init; }

        public string Sort { get; init; } = "name";
    }
}