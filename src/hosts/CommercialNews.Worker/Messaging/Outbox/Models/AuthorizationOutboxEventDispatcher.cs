using CommercialNews.Worker.Messaging.Authorization.Ports;
using CommercialNews.Worker.Messaging.Outbox.Models;

namespace CommercialNews.Worker.Messaging.Authorization.Dispatching
{
    public sealed class AuthorizationOutboxEventDispatcher : IAuthorizationOutboxEventDispatcher
    {
        private readonly ILogger<AuthorizationOutboxEventDispatcher> _logger;

        public AuthorizationOutboxEventDispatcher(
            ILogger<AuthorizationOutboxEventDispatcher> logger)
        {
            _logger = logger;
        }

        public Task DispatchAsync(
            OutboxMessageRecord message,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Authorization outbox event acknowledged. OutboxMessageId={OutboxMessageId}, EventType={EventType}, AggregateType={AggregateType}, AggregateId={AggregateId}",
                message.OutboxMessageId,
                message.EventType,
                message.AggregateType,
                message.AggregateId);

            return Task.CompletedTask;
        }
    }
}