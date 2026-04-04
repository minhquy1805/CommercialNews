using CommercialNews.BuildingBlocks.Persistence.Sql;
using Media.Application.Ports.Persistence;

namespace Media.Infrastructure.Persistence.Sql
{
    public sealed class MediaUnitOfWork : SqlUnitOfWorkBase, IMediaUnitOfWork
    {
        public MediaUnitOfWork(ISqlConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}