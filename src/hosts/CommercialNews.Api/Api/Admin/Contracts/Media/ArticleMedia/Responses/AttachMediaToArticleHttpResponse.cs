namespace CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;

public sealed class AttachMediaToArticleHttpResponse
{
    public long? ArticleMediaId { get; init; }

    public long ArticleId { get; init; }

    public long MediaId { get; init; }

    public bool Attached { get; init; }

    public bool IsPrimary { get; init; }

    public bool PrimaryChanged { get; init; }

    public int AffectedRows { get; init; }

    public int AttachmentSetVersion { get; init; }
}