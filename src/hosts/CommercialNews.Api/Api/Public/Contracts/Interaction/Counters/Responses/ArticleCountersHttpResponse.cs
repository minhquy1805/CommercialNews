namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Counters.Responses;

public sealed class GetArticleCountersHttpResponse
{
    public long ArticleId { get; init; }

    public long Views { get; init; }

    public long Likes { get; init; }

    public long Comments { get; init; }

    public bool Partial { get; init; }
}