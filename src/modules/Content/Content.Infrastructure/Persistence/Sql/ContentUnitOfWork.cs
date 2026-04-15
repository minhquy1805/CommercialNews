using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Content.Application.Ports.Persistence;

namespace Content.Infrastructure.Persistence.Sql
{
    public sealed class ContentUnitOfWork : SqlUnitOfWorkBase, IContentUnitOfWork
    {
        public ContentUnitOfWork(ISqlConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}

