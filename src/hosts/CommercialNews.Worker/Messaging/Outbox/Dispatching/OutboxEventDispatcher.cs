using CommercialNews.Worker.Messaging.Authorization.Ports;
using CommercialNews.Worker.Messaging.Email.Ports;
using CommercialNews.Worker.Messaging.Outbox.Models;
using CommercialNews.Worker.Messaging.Outbox.Ports;

namespace CommercialNews.Worker.Messaging.Outbox.Dispatching
{
    public sealed class OutboxEventDispatcher : IOutboxEventDispatcher
    {
        private readonly IOutboxEventEmailDispatcher _emailDispatcher;
        private readonly IAuthorizationOutboxEventDispatcher _authorizationDispatcher;

        public OutboxEventDispatcher(
            IOutboxEventEmailDispatcher emailDispatcher,
            IAuthorizationOutboxEventDispatcher authorizationDispatcher)
        {
            _emailDispatcher = emailDispatcher;
            _authorizationDispatcher = authorizationDispatcher;
        }

        public async Task DispatchAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (IsAuthorizationEvent(message.EventType))
            {
                await _authorizationDispatcher.DispatchAsync(message, cancellationToken);
                return;
            }

            if (IsEmailEvent(message.EventType))
            {
                await _emailDispatcher.DispatchAsync(message, cancellationToken);
                return;
            }

            throw new InvalidOperationException(
                $"Unsupported outbox event type: {message.EventType}");
        }

        private static bool IsAuthorizationEvent(string eventType)
        {
            return eventType.StartsWith("Authorization.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEmailEvent(string eventType)
        {
            return eventType.StartsWith("Identity.", StringComparison.OrdinalIgnoreCase)
                || eventType.StartsWith("Notifications.", StringComparison.OrdinalIgnoreCase);
        }
    }
}