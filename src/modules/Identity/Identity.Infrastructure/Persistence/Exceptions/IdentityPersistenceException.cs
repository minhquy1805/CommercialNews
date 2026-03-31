using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Identity.Infrastructure.Persistence.Exceptions
{
    public sealed class IdentityPersistenceException : PersistenceException
    {
        public IdentityPersistenceException(
            string code,
            string message,
            Exception? innerException = null)
            : base(code, message, innerException)
        {
        }
    }
}