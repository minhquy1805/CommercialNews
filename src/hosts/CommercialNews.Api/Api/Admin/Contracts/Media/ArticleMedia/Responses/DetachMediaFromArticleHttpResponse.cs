namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;

public sealed class DetachMediaFromArticleHttpResponse
{
    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public bool Detached { get; init; }

    public bool PrimaryCleared { get; init; }

    public int AffectedRows { get; init; }

    public int AttachmentSetVersion { get; init; }
}