using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;

namespace Interaction.Infrastructure.Persistence.Exceptions;

public sealed class InteractionPersistenceException : PersistenceException
{
    public InteractionPersistenceException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}