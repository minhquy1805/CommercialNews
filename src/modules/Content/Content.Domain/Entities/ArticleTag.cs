using Content.Domain.Exceptions;

namespace Content.Domain.Entities
{
    public sealed class ArticleTag
    {
        private ArticleTag(
            long articleId,
            long tagId,
            DateTime attachedAt,
            long? attachedByUserId)
        {
            ArticleId = articleId;
            TagId = tagId;
            AttachedAt = attachedAt;
            AttachedByUserId = attachedByUserId;
        }

        public long ArticleId { get; private set; }

        public long TagId { get; private set; }

        public DateTime AttachedAt { get; private set; }

        public long? AttachedByUserId { get; private set; }

        public static ArticleTag Attach(
            long articleId,
            long tagId,
            DateTime nowUtc,
            long? actorUserId)
        {
            ValidateArticleId(articleId);
            ValidateTagId(tagId);

            return new ArticleTag(
                articleId: articleId,
                tagId: tagId,
                attachedAt: nowUtc,
                attachedByUserId: actorUserId);
        }

        public static ArticleTag Rehydrate(
            long articleId,
            long tagId,
            DateTime attachedAt,
            long? attachedByUserId)
        {
            ValidateArticleId(articleId);
            ValidateTagId(tagId);

            return new ArticleTag(
                articleId: articleId,
                tagId: tagId,
                attachedAt: attachedAt,
                attachedByUserId: attachedByUserId);
        }

        private static void ValidateArticleId(long articleId)
        {
            if (articleId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_TAG_INVALID_ARTICLE_ID",
                    "Article id must be greater than zero.");
            }
        }

        private static void ValidateTagId(long tagId)
        {
            if (tagId <= 0)
            {
                throw new ContentDomainException(
                    "CONTENT.ARTICLE_TAG_INVALID_TAG_ID",
                    "Tag id must be greater than zero.");
            }
        }
    }
}