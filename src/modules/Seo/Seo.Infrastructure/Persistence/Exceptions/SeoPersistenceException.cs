using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Seo.Infrastructure.Persistence.Exceptions;

public sealed class SeoPersistenceException : PersistenceException
{
    public SeoPersistenceException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}