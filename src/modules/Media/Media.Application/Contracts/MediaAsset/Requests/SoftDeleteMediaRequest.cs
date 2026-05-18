namespace Media.Application.Contracts.MediaAsset.Requests;

public sealed class SoftDeleteMediaRequest
{
    public long MediaId { get; init; }

    public DateTime? RestoreUntil { get; init; }
}