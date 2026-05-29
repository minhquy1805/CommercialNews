using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Interaction.Application.Ports.Persistence;

namespace Interaction.Infrastructure.Persistence.Sql;

public sealed class InteractionUnitOfWork : SqlUnitOfWorkBase, IInteractionUnitOfWork
{
    public InteractionUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}