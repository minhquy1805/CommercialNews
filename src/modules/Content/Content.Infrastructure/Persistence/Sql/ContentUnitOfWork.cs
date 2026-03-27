using CommercialNews.BuildingBlocks.Persistence.Sql;
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

