namespace CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Responses
{
    public sealed class RestoreCategoryResponse
    {
        public long CategoryId { get; init; }
        public bool IsDeleted { get; init; }
        public long Version { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}