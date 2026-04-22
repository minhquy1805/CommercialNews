using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace CommercialNews.BuildingBlocks.Outbox.Exceptions;

public sealed class OutboxPersistenceException : PersistenceException
{
    public OutboxPersistenceException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}