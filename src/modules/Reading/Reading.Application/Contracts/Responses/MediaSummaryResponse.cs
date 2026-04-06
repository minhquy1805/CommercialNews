namespace Reading.Application.Contracts.Responses;

public sealed class MediaSummaryResponse
{
    public long MediaId { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? Alt { get; set; }

    public bool IsPrimary { get; set; }

    public int Order { get; set; }
}