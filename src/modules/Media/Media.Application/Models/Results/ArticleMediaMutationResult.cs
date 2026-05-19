namespace Media.Application.Models.Results;

public sealed record ArticleMediaMutationResult(
    int ResultCode,
    int AffectedRows,
    int? NewVersion)
{
    public bool Succeeded => ResultCode == 0;
}