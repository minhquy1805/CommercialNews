namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Responses
{
    public sealed class UpdateTagResponse
    {
        public long TagId { get; init; }

        public string Name { get; init; } = string.Empty;
        public string NameNormalized { get; init; } = string.Empty;
        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public int Version { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}