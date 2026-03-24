namespace Authorization.Application.Contracts.Responses
{
    public sealed class CheckUserHasPermissionResponseDto
    {
        public long UserId { get; init; }
        public string PermissionName { get; init; } = string.Empty;
        public string PermissionNameNormalized { get; init; } = string.Empty;
        public bool HasPermission { get; init; }
    }
}