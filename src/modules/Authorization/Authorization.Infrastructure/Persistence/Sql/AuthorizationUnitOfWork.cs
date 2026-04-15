using Authorization.Application.Ports.Persistence;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;

namespace Authorization.Infrastructure.Persistence.Sql
{
    public sealed class AuthorizationUnitOfWork : SqlUnitOfWorkBase, IAuthorizationUnitOfWork
    {
        public AuthorizationUnitOfWork(ISqlConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}