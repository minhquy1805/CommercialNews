namespace Content.Application.Contracts.Requests;

public sealed class SoftDeleteCategoryRequestDto
{
    public long CategoryId { get; init; }

    public long ExpectedVersion { get; init; }

    public long? ActorUserId { get; init; }
}