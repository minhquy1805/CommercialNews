namespace Content.Application.Contracts.Requests;

public sealed class UpdateTagRequestDto
{
    public long TagId { get; init; }

    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }

    public long ExpectedVersion { get; init; }
    public long? ActorUserId { get; init; }
}
