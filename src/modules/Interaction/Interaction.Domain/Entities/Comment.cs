using Interaction.Domain.Constants;
using Interaction.Domain.Exceptions;

namespace Interaction.Domain.Entities;

public sealed class Comment
{
    public long CommentId { get; private set; }

    public string PublicId { get; private set; } = string.Empty;
    public string ArticlePublicId { get; private set; } = string.Empty;
    public long AuthorUserId { get; private set; }

    /// <summary>
    /// Reserved for future reply-comment capability.
    /// V1-created comments must always have ParentCommentId = null.
    /// </summary>
    public long? ParentCommentId { get; private set; }

    public string Content { get; private set; } = string.Empty;
    public string Status { get; private set; } = CommentStatuses.Visible;

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Comment()
    {
    }

    /// <summary>
    /// Creates a top-level visible comment for the default V1 post-moderation flow.
    /// Valid user comments are publicly visible immediately while the article
    /// remains eligible for public interaction.
    /// </summary>
    public static Comment CreateVisible(
        string publicId,
        string articlePublicId,
        long authorUserId,
        string content,
        DateTime createdAtUtc)
    {
        ValidatePublicId(publicId);
        ValidateArticlePublicId(articlePublicId);
        ValidateAuthorUserId(authorUserId);
        ValidateContent(content);
        ValidateCreatedAtUtc(createdAtUtc);

        return new Comment
        {
            PublicId = NormalizeRequired(publicId),
            ArticlePublicId = NormalizeRequired(articlePublicId),
            AuthorUserId = authorUserId,
            ParentCommentId = null,
            Content = NormalizeRequired(content),
            Status = CommentStatuses.Visible,
            Version = 1,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };
    }

    /// <summary>
    /// Rehydrates an existing persisted comment.
    /// Status transitions are executed by authoritative persistence procedures
    /// because they require version checks and transactional workflow effects.
    /// </summary>
    public static Comment Rehydrate(
        long commentId,
        string publicId,
        string articlePublicId,
        long authorUserId,
        long? parentCommentId,
        string content,
        string status,
        long version,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc,
        DateTime? deletedAtUtc)
    {
        ValidateCommentId(commentId);
        ValidatePublicId(publicId);
        ValidateArticlePublicId(articlePublicId);
        ValidateAuthorUserId(authorUserId);
        ValidateParentCommentId(parentCommentId);
        ValidateContent(content);
        ValidateStatus(status);
        ValidateVersion(version);
        ValidatePersistedState(
            status,
            createdAtUtc,
            updatedAtUtc,
            deletedAtUtc);

        return new Comment
        {
            CommentId = commentId,
            PublicId = NormalizeRequired(publicId),
            ArticlePublicId = NormalizeRequired(articlePublicId),
            AuthorUserId = authorUserId,
            ParentCommentId = parentCommentId,
            Content = NormalizeRequired(content),
            Status = NormalizeRequired(status),
            Version = version,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            DeletedAtUtc = deletedAtUtc
        };
    }

    public bool IsVisible()
    {
        return string.Equals(
            Status,
            CommentStatuses.Visible,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsHidden()
    {
        return string.Equals(
            Status,
            CommentStatuses.Hidden,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDeleted()
    {
        return string.Equals(
            Status,
            CommentStatuses.Deleted,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCommentId(long commentId)
    {
        if (commentId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_ID",
                "Comment id must be greater than zero.");
        }
    }

    private static void ValidatePublicId(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_PUBLIC_ID_REQUIRED",
                "Comment public id is required.");
        }
    }

    private static void ValidateArticlePublicId(string articlePublicId)
    {
        if (string.IsNullOrWhiteSpace(articlePublicId))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_ARTICLE_PUBLIC_ID_REQUIRED",
                "Article public id is required.");
        }
    }

    private static void ValidateAuthorUserId(long authorUserId)
    {
        if (authorUserId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_AUTHOR_USER_ID",
                "Author user id must be greater than zero.");
        }
    }

    private static void ValidateParentCommentId(long? parentCommentId)
    {
        if (parentCommentId.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_REPLY_NOT_SUPPORTED",
                "Reply comments are not supported in Interaction V1.");
        }
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_CONTENT_REQUIRED",
                "Comment content is required.");
        }

        if (content.Trim().Length > 2000)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_CONTENT_TOO_LONG",
                "Comment content must not exceed 2000 characters.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (!CommentStatuses.IsValid(status))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_STATUS",
                "Comment status is invalid.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_VERSION",
                "Comment version must be greater than or equal to one.");
        }
    }

    private static void ValidateCreatedAtUtc(DateTime createdAtUtc)
    {
        if (createdAtUtc == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_CREATED_AT_UTC",
                "CreatedAtUtc must be a valid datetime.");
        }
    }

    private static void ValidatePersistedState(
        string status,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc,
        DateTime? deletedAtUtc)
    {
        ValidateCreatedAtUtc(createdAtUtc);

        if (updatedAtUtc.HasValue && updatedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_UPDATED_AT_UTC_ORDER",
                "UpdatedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        if (deletedAtUtc.HasValue && deletedAtUtc.Value < createdAtUtc)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_DELETED_AT_UTC_ORDER",
                "DeletedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        var isDeleted = string.Equals(
            status,
            CommentStatuses.Deleted,
            StringComparison.OrdinalIgnoreCase);

        if (isDeleted && !deletedAtUtc.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_DELETED_STATE_INVALID",
                "Deleted comment must have DeletedAtUtc.");
        }

        if (!isDeleted && deletedAtUtc.HasValue)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_NON_DELETED_STATE_INVALID",
                "Non-deleted comment must not have DeletedAtUtc.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }
}