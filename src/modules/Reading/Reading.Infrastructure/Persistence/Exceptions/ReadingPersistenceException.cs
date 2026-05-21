using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Reading.Infrastructure.Persistence.Exceptions;

public sealed class ReadingPersistenceException : PersistenceException
{
    public ReadingPersistenceException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}