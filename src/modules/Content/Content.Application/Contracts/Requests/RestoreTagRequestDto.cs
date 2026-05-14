namespace Content.Application.Contracts.Requests;

public sealed class RestoreTagRequestDto
{
    public long TagId { get; init; }
    public long ExpectedVersion { get; init; }
    public long? ActorUserId { get; init; }
}
