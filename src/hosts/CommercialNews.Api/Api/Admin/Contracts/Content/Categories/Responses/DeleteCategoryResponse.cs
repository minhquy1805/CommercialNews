namespace CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Responses
{
    public sealed class DeleteCategoryResponse
    {
        public long CategoryId { get; init; }
        public bool IsDeleted { get; init; }
        public int Version { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}