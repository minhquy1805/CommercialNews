namespace Content.Application.Outbox;

public static class ContentIntegrationEventTypes
{
    public const string ArticleCreated = "content.article_created";
    public const string ArticleUpdated = "content.article_updated";
    public const string ArticlePublished = "content.article_published";
    public const string ArticleUnpublished = "content.article_unpublished";
    public const string ArticleArchived = "content.article_archived";
    public const string ArticleSoftDeleted = "content.article_soft_deleted";
}