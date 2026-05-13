namespace Content.Application.Contracts.Responses;

public sealed class ArticleTagItemDto
{
    public long ArticleId { get; init; }

    public long TagId { get; init; }

    public DateTime AttachedAt { get; init; }

    public long? AttachedByUserId { get; init; }
}