namespace Reading.Application.Models.QueryModels;

public sealed class ReadingCategoryResultItem
{
    public long CategoryId { get; init; }

    public string Name { get; init; } = string.Empty;
}