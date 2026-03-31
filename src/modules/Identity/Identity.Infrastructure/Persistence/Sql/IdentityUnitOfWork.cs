using CommercialNews.BuildingBlocks.Persistence.Sql;
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