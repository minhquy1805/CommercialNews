namespace Content.Application.Contracts.Requests
{
    public sealed class CreateTagRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
        public long? ActorUserId { get; init; }
    }
}