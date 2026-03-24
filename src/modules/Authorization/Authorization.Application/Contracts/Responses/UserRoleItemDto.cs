namespace Authorization.Application.Contracts.Responses
{
    public sealed class UserRoleItemDto
    {
        public long RoleId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string NameNormalized { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsSystem { get; init; }
        public bool IsActive { get; init; }
        public DateTime AssignedAt { get; init; }
        public long? AssignedByUserId { get; init; }
    }
}
