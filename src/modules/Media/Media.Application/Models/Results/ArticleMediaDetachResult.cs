namespace Media.Application.Models.Results;

public sealed record ArticleMediaDetachResult(
    int ResultCode,
    int AffectedRows,
    bool PrimaryCleared,
    int? NewVersion)
{
    public bool Succeeded => ResultCode == 0;
}