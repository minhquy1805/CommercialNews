namespace CommercialNews.Api.Api.Admin.Contracts.Requests
{
    public sealed class CheckUserHasPermissionHttpRequest
    {
        public string PermissionName { get; init; } = string.Empty;
    }
}