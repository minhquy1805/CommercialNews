namespace Reading.Application.Contracts.Responses;

public sealed class ArticleCountersResponse
{
    public long Views { get; set; }

    public long Likes { get; set; }

    public bool CountersPartial { get; set; }
}