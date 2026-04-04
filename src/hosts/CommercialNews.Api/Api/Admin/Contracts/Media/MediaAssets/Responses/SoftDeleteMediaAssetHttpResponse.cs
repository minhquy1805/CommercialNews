namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;

public sealed class SoftDeleteMediaAssetHttpResponse
{
    public long MediaId { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? RestoreUntil { get; init; }
    public int AffectedRows { get; init; }
}