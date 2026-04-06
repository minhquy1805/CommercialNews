namespace Reading.Application.Contracts.Responses;

public sealed class CategorySummaryResponse
{
    public long CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;
}