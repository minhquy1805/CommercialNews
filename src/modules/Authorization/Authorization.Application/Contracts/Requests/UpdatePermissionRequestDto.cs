namespace Authorization.Application.Contracts.Requests
{
    public sealed class UpdatePermissionRequestDto
    {
        public long PermissionId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Module { get; init; }
    }
}