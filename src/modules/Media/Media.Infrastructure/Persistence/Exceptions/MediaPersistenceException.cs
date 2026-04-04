using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Media.Infrastructure.Persistence.Exceptions;

public sealed class MediaPersistenceException : PersistenceException
{
    public MediaPersistenceException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}