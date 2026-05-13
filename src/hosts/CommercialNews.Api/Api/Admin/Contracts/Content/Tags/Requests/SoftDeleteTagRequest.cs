namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Requests
{
    public sealed class SoftDeleteTagRequest
    {
        public long ExpectedVersion { get; init; }
    }
}
