namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Responses
{
    public sealed class DeleteTagResponse
    {
        public long TagId { get; init; }
        public bool IsDeleted { get; init; }

        public int Version { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}