namespace Reading.Application.Contracts.Responses;

public sealed class TagSummaryResponse
{
    public long TagId { get; set; }

    public string Name { get; set; } = string.Empty;
}