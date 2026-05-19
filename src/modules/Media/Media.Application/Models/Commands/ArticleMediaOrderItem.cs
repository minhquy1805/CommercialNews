namespace Media.Application.Models.Commands;

public sealed record ArticleMediaOrderItem(
    long MediaId,
    int SortOrder);