namespace Interaction.Domain.Entities;

using Interaction.Domain.Enums;
using Interaction.Domain.Exceptions;

public sealed class Comment
{
    public long CommentId { get; private set; }

    public long ArticleId { get; private set; }
    public long UserId { get; private set; }
    public long? ParentCommentId { get; private set; }

    public string Content { get; private set; } = string.Empty;
    public string Status { get; private set; } = CommentStatus.Visible;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public long? DeletedByUserId { get; private set; }

    public int EditCount { get; private set; }

    private Comment()
    {
    }

    public static Comment Create(
        long articleId,
        long userId,
        long? parentCommentId,
        string content,
        DateTime nowUtc)
    {
        ValidateArticleId(articleId);
        ValidateUserId(userId);
        ValidateParentCommentId(parentCommentId);
        ValidateContent(content);

        return new Comment
        {
            ArticleId = articleId,
            UserId = userId,
            ParentCommentId = parentCommentId,
            Content = NormalizeRequired(content),
            Status = CommentStatus.Visible,
            CreatedAt = nowUtc,
            UpdatedAt = null,
            DeletedAt = null,
            DeletedByUserId = null,
            EditCount = 0
        };
    }

    public static Comment Rehydrate(
        long commentId,
        long articleId,
        long userId,
        long? parentCommentId,
        string content,
        string status,
        DateTime createdAt,
        DateTime? updatedAt,
        DateTime? deletedAt,
        long? deletedByUserId,
        int editCount)
    {
        if (commentId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_ID",
                "Comment id must be greater than zero.");
        }

        ValidateArticleId(articleId);
        ValidateUserId(userId);
        ValidateParentCommentId(parentCommentId);
        ValidateContent(content);
        ValidateStatus(status);
        ValidateState(status, createdAt, updatedAt, deletedAt, deletedByUserId, editCount);

        return new Comment
        {
            CommentId = commentId,
            ArticleId = articleId,
            UserId = userId,
            ParentCommentId = parentCommentId,
            Content = NormalizeRequired(content),
            Status = status.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            DeletedByUserId = deletedByUserId,
            EditCount = editCount
        };
    }

    public void Update(
        string content,
        DateTime nowUtc)
    {
        EnsureNotDeleted();
        ValidateContent(content);

        Content = NormalizeRequired(content);
        UpdatedAt = nowUtc;
        EditCount++;
    }

    public void MarkHidden(DateTime nowUtc)
    {
        EnsureNotDeleted();

        Status = CommentStatus.Hidden;
        UpdatedAt = nowUtc;
    }

    public void MarkPending(DateTime nowUtc)
    {
        EnsureNotDeleted();

        Status = CommentStatus.Pending;
        UpdatedAt = nowUtc;
    }

    public void MarkVisible(DateTime nowUtc)
    {
        EnsureNotDeleted();

        Status = CommentStatus.Visible;
        UpdatedAt = nowUtc;
    }

    public void SoftDelete(
        DateTime nowUtc,
        long? deletedByUserId)
    {
        if (Status == CommentStatus.Deleted)
        {
            return;
        }

        if (deletedByUserId.HasValue && deletedByUserId.Value <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_DELETED_BY_USER_ID",
                "DeletedByUserId must be greater than zero when provided.");
        }

        Status = CommentStatus.Deleted;
        DeletedAt = nowUtc;
        DeletedByUserId = deletedByUserId;
        UpdatedAt = nowUtc;
    }

    private void EnsureNotDeleted()
    {
        if (Status == CommentStatus.Deleted)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_ALREADY_DELETED",
                "Deleted comment cannot be modified.");
        }
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateUserId(long userId)
    {
        if (userId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_USER_ID",
                "User id must be greater than zero.");
        }
    }

    private static void ValidateParentCommentId(long? parentCommentId)
    {
        if (parentCommentId.HasValue && parentCommentId.Value <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_PARENT_COMMENT_ID",
                "Parent comment id must be greater than zero when provided.");
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
        if (!CommentStatus.IsValid(status))
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_STATUS",
                "Comment status is invalid.");
        }
    }

    private static void ValidateState(
        string status,
        DateTime createdAt,
        DateTime? updatedAt,
        DateTime? deletedAt,
        long? deletedByUserId,
        int editCount)
    {
        if (createdAt == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_CREATED_AT",
                "CreatedAt must be a valid UTC datetime.");
        }

        if (updatedAt.HasValue && updatedAt.Value < createdAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_UPDATED_AT_ORDER",
                "UpdatedAt must be greater than or equal to CreatedAt.");
        }

        if (deletedAt.HasValue && deletedAt.Value < createdAt)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_DELETED_AT_ORDER",
                "DeletedAt must be greater than or equal to CreatedAt.");
        }

        if (editCount < 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_EDIT_COUNT",
                "EditCount must be greater than or equal to zero.");
        }

        if (string.Equals(status, CommentStatus.Deleted, StringComparison.OrdinalIgnoreCase))
        {
            if (!deletedAt.HasValue)
            {
                throw new InteractionDomainException(
                    "INTERACTION.COMMENT_DELETED_STATE_INVALID",
                    "Deleted comment must have DeletedAt.");
            }
        }
        else
        {
            if (deletedAt.HasValue)
            {
                throw new InteractionDomainException(
                    "INTERACTION.COMMENT_NON_DELETED_STATE_INVALID",
                    "Non-deleted comment must not have DeletedAt.");
            }

            if (deletedByUserId.HasValue)
            {
                throw new InteractionDomainException(
                    "INTERACTION.COMMENT_NON_DELETED_BY_USER_INVALID",
                    "Non-deleted comment must not have DeletedByUserId.");
            }
        }

        if (deletedByUserId.HasValue && deletedByUserId.Value <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.COMMENT_INVALID_DELETED_BY_USER_ID",
                "DeletedByUserId must be greater than zero when provided.");
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }
}