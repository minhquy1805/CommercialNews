namespace CommercialNews.Api.Api.Admin.Contracts.Content.Categories.Requests
{
    public sealed class SoftDeleteCategoryRequest
    {
        public long ExpectedVersion { get; init; }
    }
}
