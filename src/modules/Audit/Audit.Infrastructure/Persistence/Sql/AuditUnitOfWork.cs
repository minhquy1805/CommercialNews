using Audit.Application.Ports.Persistence;
using CommercialNews.BuildingBlocks.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;

namespace Audit.Infrastructure.Persistence.Sql;

public sealed class AuditUnitOfWork : SqlUnitOfWorkBase, IAuditUnitOfWork
{
    public AuditUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}