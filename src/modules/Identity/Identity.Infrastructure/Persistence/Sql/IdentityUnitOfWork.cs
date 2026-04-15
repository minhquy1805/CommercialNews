using CommercialNews.BuildingBlocks.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Identity.Application.Ports.Persistence;

namespace Identity.Infrastructure.Persistence.Sql
{
    public sealed class IdentityUnitOfWork : SqlUnitOfWorkBase, IIdentityUnitOfWork
    {
        public IdentityUnitOfWork(ISqlConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}