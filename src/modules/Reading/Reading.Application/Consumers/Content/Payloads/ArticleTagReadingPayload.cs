namespace Reading.Application.Consumers.Content.Payloads;

public sealed class ArticleTagReadingPayload
{
    public long TagId { get; init; }

    public string? TagPublicId { get; init; }

    public string Name { get; init; } = string.Empty;
}
