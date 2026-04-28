using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;

namespace CommercialNews.BuildingBlocks.Outbox.Ports;

public interface IOutboxUnitOfWork : ISqlUnitOfWork
{
}