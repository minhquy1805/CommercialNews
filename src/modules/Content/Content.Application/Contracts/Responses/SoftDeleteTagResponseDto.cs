namespace Content.Application.Contracts.Responses;

public sealed class SoftDeleteTagResponseDto
{
    public long TagId { get; init; }

    public bool IsDeleted { get; init; }

    public bool IsActive { get; init; }

    public long Version { get; init; }

    public DateTime UpdatedAt { get; init; }

    public DateTime? DeletedAt { get; init; }
}