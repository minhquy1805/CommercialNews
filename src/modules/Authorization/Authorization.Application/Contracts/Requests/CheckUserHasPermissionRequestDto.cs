namespace Authorization.Application.Contracts.Requests
{
    public sealed class CheckUserHasPermissionRequestDto
    {
        public long UserId { get; init; }
        public string PermissionName { get; init; } = string.Empty;
    }
}

