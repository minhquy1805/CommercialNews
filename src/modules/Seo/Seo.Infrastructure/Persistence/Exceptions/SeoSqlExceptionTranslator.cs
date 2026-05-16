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

        if (message.Contains("UQ_SeoMetadata_Scope_Resource", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.METADATA_ALREADY_EXISTS",
                message: "SEO metadata already exists for this resource in this scope.",
                innerException: exception);
        }

        if (message.Contains("UQ_SlugRegistry_Scope_Resource", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.ACTIVE_ROUTE_ALREADY_EXISTS",
                message: "A slug route already exists for this resource in this scope.",
                innerException: exception);
        }

        if (message.Contains("UX_SlugRegistry_Scope_Slug_Active", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.SLUG_CONFLICT",
                message: "The slug is already in use for this scope.",
                innerException: exception);
        }

        return new SeoPersistenceException(
            code: "SEO.VALIDATION_FAILED",
            message: "A persistence uniqueness constraint was violated.",
            innerException: exception);
    }

    private static Exception MapForeignKeyOrCheckConstraint(SqlException exception)
    {
        string message = exception.Message;

        if (message.Contains("FK_SeoMetadata_UpdatedByUser", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_SlugRegistry_CreatedByUser", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("FK_SlugRegistry_UpdatedByUser", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.VALIDATION_FAILED",
                message: "The referenced actor user does not exist.",
                innerException: exception);
        }

        if (message.Contains("CK_SeoMetadata_Scope", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SlugRegistry_Scope", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_SCOPE",
                message: "Scope is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_SeoMetadata_ResourceType", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SlugRegistry_ResourceType", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_RESOURCE_TYPE",
                message: "Resource type is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_SeoMetadata_ResourcePublicId_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SlugRegistry_ResourcePublicId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_RESOURCE_PUBLIC_ID",
                message: "Resource public id is required.",
                innerException: exception);
        }

        if (message.Contains("CK_SlugRegistry_Slug_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_Slug_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_SLUG",
                message: "Slug is required.",
                innerException: exception);
        }

        if (message.Contains("CK_SeoMetadata_CanonicalUrl_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SlugRegistry_CanonicalUrl_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_CANONICAL_URL",
                message: "Canonical URL is invalid.",
                innerException: exception);
        }

        if (message.Contains("CK_SeoMetadata_MetaTitle_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_MetaDescription_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_OgTitle_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_OgDescription_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_OgImageUrl_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_TwitterTitle_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_TwitterDescription_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_TwitterImageUrl_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SeoMetadata_Robots_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.VALIDATION_FAILED",
                message: "SEO metadata contains an invalid blank value.",
                innerException: exception);
        }

        if (message.Contains("CK_SeoMetadata_SourceAggregateVersion_Positive", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SlugRegistry_SourceAggregateVersion_Positive", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_SOURCE_AGGREGATE_VERSION",
                message: "Source aggregate version must be greater than zero.",
                innerException: exception);
        }

        if (message.Contains("CK_SeoMetadata_LastAppliedMessageId_NotBlank", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SlugRegistry_LastAppliedMessageId_NotBlank", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.INVALID_LAST_APPLIED_MESSAGE_ID",
                message: "Last applied message id is invalid.",
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

        if (message.Contains("CK_SeoMetadata_UpdatedAtUtc", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("CK_SlugRegistry_UpdatedAtUtc", StringComparison.OrdinalIgnoreCase))
        {
            return new SeoPersistenceException(
                code: "SEO.VALIDATION_FAILED",
                message: "Updated time is invalid for the current SEO state.",
                innerException: exception);
        }

        return new SeoPersistenceException(
            code: "SEO.VALIDATION_FAILED",
            message: "A foreign key or check constraint was violated.",
            innerException: exception);
    }
}