namespace CommercialNews.Api.Api.Admin.Contracts.Content.Tags.Requests
{
    public sealed class CreateTagRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }
}