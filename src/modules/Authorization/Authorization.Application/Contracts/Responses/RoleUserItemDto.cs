namespace Authorization.Application.Contracts.Responses
{
    public sealed class RoleUserItemDto
    {
        public long UserId { get; init; }
        public string PublicId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? FullName { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool IsEmailVerified { get; init; }
        public DateTime AssignedAt { get; init; }
        public long? AssignedByUserId { get; init; }
    }
}