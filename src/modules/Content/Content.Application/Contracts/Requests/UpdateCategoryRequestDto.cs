namespace Content.Application.Contracts.Requests;

public sealed class UpdateCategoryRequestDto
{
    public long CategoryId { get; init; }
    public long? ParentCategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public long ExpectedVersion { get; init; }
    public long? ActorUserId { get; init; }
}
