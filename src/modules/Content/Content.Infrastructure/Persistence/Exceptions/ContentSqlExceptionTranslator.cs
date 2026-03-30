using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using Microsoft.Data.SqlClient;

namespace Content.Infrastructure.Persistence.Exceptions
{
    public sealed class ContentSqlExceptionTranslator : SqlExceptionTranslatorBase
    {
        public override Exception Translate(SqlException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.Number switch
            {
                54227 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54230 => new ContentPersistenceException(
                    code: "CONTENT.CATEGORY_DELETE_BLOCKED_BY_ARTICLES",
                    message: "Category cannot be deleted because active articles still reference it.",
                    innerException: exception),

                54231 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                54232 => new ContentPersistenceException(
                    code: "CONTENT.CONCURRENCY_CONFLICT",
                    message: "The content resource was modified by another operation.",
                    innerException: exception),

                _ => exception
            };
        }
    }
}