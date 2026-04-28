using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;

namespace CommercialNews.BuildingBlocks.Outbox.Persistence;

public sealed class OutboxUnitOfWork : SqlUnitOfWorkBase, IOutboxUnitOfWork
{
    public OutboxUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}