namespace Interaction.Application.Contracts.Counters.Requests;

public sealed class GetArticleCountersRequest
{
    public long ArticleId { get; init; }
}