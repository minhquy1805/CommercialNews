using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Reading.Application.Ports.Persistence;

namespace Reading.Infrastructure.Persistence.Sql;

public sealed class ReadingUnitOfWork : SqlUnitOfWorkBase, IReadingUnitOfWork
{
    public ReadingUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}