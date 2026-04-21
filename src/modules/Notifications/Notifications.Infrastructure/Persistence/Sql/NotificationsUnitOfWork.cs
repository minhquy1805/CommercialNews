using CommercialNews.BuildingBlocks.Persistence.Sql;
using CommercialNews.BuildingBlocks.Persistence.Sql.Connections;
using CommercialNews.BuildingBlocks.Persistence.Sql.Transactions;
using Notifications.Application.Ports.Transactions;

namespace Notifications.Infrastructure.Persistence.Sql;

public sealed class NotificationsUnitOfWork : SqlUnitOfWorkBase, INotificationsUnitOfWork
{
    public NotificationsUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}