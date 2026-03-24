namespace CommercialNews.Api.Api.Admin.Contracts.Requests
{
    public sealed class CreateRoleHttpRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsSystem { get; init; }
    }
}