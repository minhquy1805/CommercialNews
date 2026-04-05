using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Seo.Infrastructure.Persistence.Exceptions;

public sealed class SeoSqlExceptionTranslator : SqlExceptionTranslatorBase
{
    public override Exception Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Number switch
        {
            2601 or 2627 => MapUniqueConstraint(exception),
            547 => MapForeignKeyOrCheckConstraint(exception),

            _ => new SeoPersistenceException(
                code: "SEO.VALIDATION_FAILED",
                message: "An unexpected SQL persistence error occurred.",
                innerException: exception)
        };
    }

    private static Exception MapUniqueConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("UQ_SeoMetadata_ArticleId", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.METADATA_ALREADY_EXISTS",
                message: "SEO metadata already exists for this article.",
                innerException: exception);
        }

        if (message.Contains("UQ_SlugRegistry_Scope_Slug", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.SLUG_CONFLICT",
                message: "The slug is already in use for this scope.",
                innerException: exception);
        }

        if (message.Contains("UX_SlugRegistry_ArticleId_Scope_Active", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.ACTIVE_ROUTE_ALREADY_EXISTS",
                message: "Another active slug already exists for this article in the same scope.",
                innerException: exception);
        }

        return new SeoPersistenceException(
            code: "SEO.VALIDATION_FAILED",
            message: "A persistence constraint was violated.",
            innerException: exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("FK_SeoMetadata_Article", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_SlugRegistry_Article", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.ARTICLE_NOT_FOUND",
                message: "The referenced article does not exist.",
                innerException: exception);
        }

        if (message.Contains("FK_SeoMetadata_UpdatedByUser", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_SlugRegistry_CreatedByUser", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_SlugRegistry_UpdatedByUser", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.VALIDATION_FAILED",
                message: "The referenced actor user does not exist.",
                innerException: exception);
        }

        if (message.Contains("CK_SlugRegistry_Scope", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_SCOPE",
                message: "Scope is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_SlugRegistry_Slug_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_SLUG",
                message: "Slug is required.",
                innerException: exception);
        }

        if (message.Contains("CK_SlugRegistry_Version_Positive", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_Version_Positive", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.VERSION_MISMATCH",
                message: "The version is invalid.",
                innerException: exception);
        }

        return new SeoPersistenceException(
            code: "SEO.VALIDATION_FAILED",
            message: "A foreign key or check constraint was violated.",
            innerException: exception);
    }
}