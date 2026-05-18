namespace Media.Application.Models.Results;

public sealed record ArticleMediaAttachResult(
    int ResultCode,
    long? ArticleMediaId,
    int AffectedRows,
    bool PrimaryChanged,
    int? NewVersion)
{
    public bool Succeeded => ResultCode == 0;
}