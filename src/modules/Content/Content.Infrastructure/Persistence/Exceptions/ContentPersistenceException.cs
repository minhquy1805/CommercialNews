using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Content.Infrastructure.Persistence.Exceptions
{
    public sealed class ContentPersistenceException : PersistenceException
    {
        public ContentPersistenceException(
            string code,
            string message,
            Exception? innerException = null)
            : base(code, message, innerException)
        {
        }
    }
}