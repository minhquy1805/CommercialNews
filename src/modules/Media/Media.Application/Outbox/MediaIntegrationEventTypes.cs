namespace Media.Application.Outbox;

public static class MediaIntegrationEventTypes
{
    public const string AssetRegistered = "media.asset_registered";
    public const string AssetUpdated = "media.asset_updated";
    public const string AssetSoftDeleted = "media.asset_soft_deleted";
    public const string AssetRestored = "media.asset_restored";

    public const string ArticleMediaAttached = "media.article_media_attached";
    public const string ArticleMediaDetached = "media.article_media_detached";
    public const string ArticleMediaReordered = "media.article_media_reordered";
    public const string ArticlePrimaryMediaSet = "media.article_primary_media_set";
}