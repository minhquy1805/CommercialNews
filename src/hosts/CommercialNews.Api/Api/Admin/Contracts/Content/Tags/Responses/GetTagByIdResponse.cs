namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Responses
{
    public sealed class GetTagByIdResponse
    {
        public long TagId { get; init; }
        public string PublicId { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;
        public string NameNormalized { get; init; } = string.Empty;
        public string? Description { get; init; }

        public bool IsActive { get; init; }
        public bool IsDeleted { get; init; }

        public long Version { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}