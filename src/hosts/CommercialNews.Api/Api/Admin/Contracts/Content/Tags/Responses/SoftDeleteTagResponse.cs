namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Responses
{
    public sealed class SoftDeleteTagResponse
    {
        public long TagId { get; init; }

        public bool IsDeleted { get; init; }

        public bool IsActive { get; init; }

        public long Version { get; init; }

        public DateTime UpdatedAt { get; init; }

        public DateTime? DeletedAt { get; init; }
    }
}
