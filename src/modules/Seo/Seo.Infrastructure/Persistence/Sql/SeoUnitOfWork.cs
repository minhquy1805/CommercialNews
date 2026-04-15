using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Seo.Application.Ports.Persistence;

namespace Seo.Infrastructure.Persistence.Sql;

public sealed class SeoUnitOfWork : SqlUnitOfWorkBase, ISeoUnitOfWork
{
    public SeoUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}