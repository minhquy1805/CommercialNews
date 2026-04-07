namespace Interaction.Application.Models.QueryModels;

public sealed class ArticleCountersResult
{
    public long ArticleId { get; init; }

    public long Views { get; init; }

    public long Likes { get; init; }

    public long Comments { get; init; }

    public bool Partial { get; init; }
}