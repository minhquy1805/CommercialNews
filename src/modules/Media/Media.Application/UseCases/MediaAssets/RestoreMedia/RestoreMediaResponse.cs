namespace Media.Application.Contracts.MediaAsset.Responses;

public sealed class RestoreMediaResponse
{
    public long MediaId { get; init; }

    public bool IsRestored { get; init; }

    public int AffectedRows { get; init; }
}