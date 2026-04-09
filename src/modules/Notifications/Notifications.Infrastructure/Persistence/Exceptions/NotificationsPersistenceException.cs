using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Notifications.Infrastructure.Persistence.Exceptions;

public sealed class NotificationsPersistenceException : PersistenceException
{
    public NotificationsPersistenceException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}