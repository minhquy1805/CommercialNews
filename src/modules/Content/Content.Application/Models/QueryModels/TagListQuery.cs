namespace Content.Application.Models.QueryModels;

public sealed class TagListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Keyword { get; init; }

    public bool? IsActive { get; init; }

    public bool IsDeleted { get; init; }

    public string Sort { get; init; } = "name";
}
