namespace CommercialNews.Api.Api.Public.Contracts.Reading.Responses;

public sealed class ArticleCountersHttpResponse
{
    public long Views { get; init; }

    public long Likes { get; init; }

    public long VisibleCommentCount { get; init; }

    public bool CountersPartial { get; init; }
}
