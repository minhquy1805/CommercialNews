using CommercialNews.BuildingBlocks.Persistence.Sql;
using Notifications.Application.Ports.Persistence.Transactions;

namespace Notifications.Infrastructure.Persistence.Sql;

public sealed class NotificationsUnitOfWork : SqlUnitOfWorkBase, INotificationsUnitOfWork
{
    public NotificationsUnitOfWork(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }
}