namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Requests
{
    public sealed class RestoreTagRequest
    {
        public long ExpectedVersion { get; init; }
    }
}