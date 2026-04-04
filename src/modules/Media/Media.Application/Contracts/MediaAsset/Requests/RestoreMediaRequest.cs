namespace Media.Application.Contracts.MediaAsset.Requests;

public sealed class RestoreMediaRequest
{
    public long MediaId { get; init; }

    public long? ActorUserId { get; init; }
}