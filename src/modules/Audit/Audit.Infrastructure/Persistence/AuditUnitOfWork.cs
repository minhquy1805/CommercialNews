using Audit.Application.Abstractions.Persistence;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;

namespace Audit.Infrastructure.Persistence;

public sealed class AuditUnitOfWork : SqlUnitOfWorkBase, IAuditUnitOfWork
{
    public AuditUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}
