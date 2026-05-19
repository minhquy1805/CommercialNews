namespace Media.Application.Models.Results;

public sealed record MediaAssetMutationResult(
    int ResultCode,
    int AffectedRows,
    int? NewVersion,
    int PrimaryClearedCount = 0)
{
    public bool Succeeded => ResultCode == 0;
}