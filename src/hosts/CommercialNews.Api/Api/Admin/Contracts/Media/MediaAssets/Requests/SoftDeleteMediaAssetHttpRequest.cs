namespace CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Requests;

public sealed class SoftDeleteMediaAssetHttpRequest
{
    public DateTime? RestoreUntil { get; init; }
}