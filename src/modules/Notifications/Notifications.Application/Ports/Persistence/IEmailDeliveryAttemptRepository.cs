using Notifications.Domain.Entities;

namespace Notifications.Application.Ports.Persistence;

public interface IEmailDeliveryAttemptRepository
{
    Task<long> InsertAsync(
        EmailDeliveryAttempt emailDeliveryAttempt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailDeliveryAttempt>> GetByEmailDeliveryIdAsync(
        long emailDeliveryId,
        CancellationToken cancellationToken = default);
}