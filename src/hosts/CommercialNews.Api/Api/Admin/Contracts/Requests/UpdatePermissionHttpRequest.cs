namespace CommercialNews.Api.Api.Admin.Contracts.Requests
{
    public sealed class UpdatePermissionHttpRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Module { get; init; }
    }
}