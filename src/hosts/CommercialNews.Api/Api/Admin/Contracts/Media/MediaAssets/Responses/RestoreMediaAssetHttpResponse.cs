namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;

public sealed class RestoreMediaAssetHttpResponse
{
    public long MediaId { get; init; }
    public bool IsRestored { get; init; }
    public int AffectedRows { get; init; }
}