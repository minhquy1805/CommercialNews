using CommercialNews.BuildingBlocks.Persistence.Sql;
using Interaction.Application.Ports.Persistence.Transactions;

namespace Interaction.Infrastructure.Persistence.Sql;

public sealed class InteractionUnitOfWork : SqlUnitOfWorkBase, IInteractionUnitOfWork
{
    public InteractionUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}