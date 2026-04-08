namespace CommercialNews.Api.Api.Public.Contracts.Interaction.Counters.Requests;

public sealed class GetArticleCountersHttpRequest
{
    public long ArticleId { get; init; }
}