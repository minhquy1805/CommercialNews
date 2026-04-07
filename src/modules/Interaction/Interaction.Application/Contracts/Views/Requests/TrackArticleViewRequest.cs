namespace Interaction.Application.Contracts.Views.Requests;

public sealed class TrackArticleViewRequest
{
    public long ArticleId { get; init; }

    public long? UserId { get; init; }

    public string? VisitorKey { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}