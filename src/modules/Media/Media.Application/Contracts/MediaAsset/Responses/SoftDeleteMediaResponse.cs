namespace Media.Application.Contracts.MediaAsset.Responses;

public sealed class SoftDeleteMediaResponse
{
    public long MediaId { get; init; }

    public bool IsDeleted { get; init; }
    public DateTime? RestoreUntil { get; init; }

    public int AffectedRows { get; init; }
}