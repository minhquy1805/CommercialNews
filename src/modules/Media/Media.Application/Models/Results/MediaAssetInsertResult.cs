namespace Media.Application.Models.Results;

public sealed record MediaAssetInsertResult(
    long MediaId,
    int NewVersion);