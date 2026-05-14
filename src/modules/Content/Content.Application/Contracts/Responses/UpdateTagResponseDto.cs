namespace Content.Application.Contracts.Responses;

public sealed class UpdateTagResponseDto
{
    public long TagId { get; init; }

    public string Name { get; init; } = string.Empty;
    public string NameNormalized { get; init; } = string.Empty;
    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public long Version { get; init; }
    public DateTime UpdatedAt { get; init; }
}
