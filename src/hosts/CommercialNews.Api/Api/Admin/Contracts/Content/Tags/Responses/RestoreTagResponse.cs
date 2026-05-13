namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Responses
{
    public sealed class RestoreTagResponse
    {
        public long TagId { get; init; }
        public bool IsDeleted { get; init; }

        public long Version { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}