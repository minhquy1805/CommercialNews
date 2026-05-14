namespace Content.Application.Contracts.Responses;

public sealed class RestoreCategoryResponseDto
{
    public long CategoryId { get; init; }

    public bool IsDeleted { get; init; }

    public bool IsActive { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAt { get; init; }
}