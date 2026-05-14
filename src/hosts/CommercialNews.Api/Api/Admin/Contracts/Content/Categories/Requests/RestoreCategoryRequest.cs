namespace CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Requests
{
    public sealed class RestoreCategoryRequest
    {
        public long ExpectedVersion { get; init; }
    }
}