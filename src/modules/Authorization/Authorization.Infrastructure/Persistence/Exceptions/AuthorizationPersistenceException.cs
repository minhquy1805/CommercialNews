using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Authorization.Infrastructure.Persistence.Exceptions
{
    public sealed class AuthorizationPersistenceException : PersistenceException
    {
        public AuthorizationPersistenceException(
            string code,
            string message,
            Exception? innerException = null)
            : base(code, message, innerException)
        {
        }
    }
}