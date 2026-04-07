using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Interaction.Infrastructure.Persistence.Exceptions;

public sealed class InteractionSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),

            _ => new InteractionPersistenceException(
                code: "INTERACTION.VALIDATION_FAILED",
                message: "An unexpected SQL persistence error occurred.",
                innerException: exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_ArticleLike_ArticleId_UserId", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.LIKE_ALREADY_EXISTS",
                message: "A like record already exists for this article and user.",
                innerException: exception);
        }

        return new InteractionPersistenceException(
            code: "INTERACTION.VALIDATION_FAILED",
            message: "A persistence constraint was violated.",
            innerException: exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("FK_ArticleViewEvent_Article", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleLike_Article", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_Comment_Article", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleInteractionStats_Article", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.ARTICLE_NOT_FOUND",
                message: "The referenced article does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_ArticleViewEvent_User", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_ArticleLike_User", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_Comment_User", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_Comment_DeletedByUser", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.VALIDATION_FAILED",
                message: "The referenced user does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_Comment_ParentComment", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.PARENT_COMMENT_NOT_FOUND",
                message: "The referenced parent comment does not exist.",
                innerException: exception);
        }

        if (message.Contains("CK_Comment_Content_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.COMMENT_CONTENT_REQUIRED",
                message: "Comment content is required.",
                innerException: exception);
        }

        if (message.Contains("CK_Comment_Status", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.COMMENT_INVALID_STATUS",
                message: "Comment status is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_Comment_EditCount", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.COMMENT_INVALID_EDIT_COUNT",
                message: "Comment edit count is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_Comment_DeletedState", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_Comment_DeletedAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_Comment_UpdatedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.COMMENT_INVALID_STATE",
                message: "Comment state is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_ArticleLike_State", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleLike_UpdatedAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleLike_UnlikedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.LIKE_INVALID_STATE",
                message: "Article like state is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_ArticleViewEvent_VisitorKey_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleViewEvent_IpAddress_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleViewEvent_UserAgent_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.VIEW_INVALID_METADATA",
                message: "Article view metadata is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_ArticleInteractionStats_ViewsTotal", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleInteractionStats_LikesTotal", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleInteractionStats_CommentsTotal", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleInteractionStats_PopularityScore", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleInteractionStats_UpdatedAt", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_ArticleInteractionStats_LastAggregatedAt", StringComparison.OrdinalIgnoreCase))
        {
            return new InteractionPersistenceException(
                code: "INTERACTION.STATS_INVALID_STATE",
                message: "Interaction statistics state is invalid.",
                innerException: exception);
        }

        return new InteractionPersistenceException(
            code: "INTERACTION.VALIDATION_FAILED",
            message: "A foreign key or check constraint was violated.",
            innerException: exception);
    }
}