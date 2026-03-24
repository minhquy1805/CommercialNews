namespace Authorization.Application.Contracts.Requests
{
    public sealed class UpdateRoleRequestDto
    {
        public long RoleId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}

