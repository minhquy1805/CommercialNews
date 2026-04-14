using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Audit.Infrastructure.Persistence.Exceptions;

public sealed class AuditPersistenceException : PersistenceException
{
    public AuditPersistenceException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}