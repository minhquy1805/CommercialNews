namespace CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Responses
{
    public sealed class SoftDeleteCategoryResponse
    {
        public long CategoryId { get; init; }

        public bool IsDeleted { get; init; }

        public bool IsActive { get; init; }

        public long Version { get; init; }

        public DateTime UpdatedAt { get; init; }

        public DateTime? DeletedAt { get; init; }
    }
}
