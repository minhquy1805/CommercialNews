namespace Content.Application.Contracts.Responses
{
    public sealed class CreateTagResponseDto
    {
        public long TagId { get; init; }
        public string PublicId { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;
        public string NameNormalized { get; init; } = string.Empty;
        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public int Version { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}