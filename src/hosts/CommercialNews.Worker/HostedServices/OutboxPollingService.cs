using CommercialNews.Worker.Messaging.Outbox.Models;
using CommercialNews.Worker.Messaging.Outbox.Ports;

namespace CommercialNews.Worker.HostedServices
{
    public sealed class OutboxPollingService : BackgroundService
    {
        private readonly ILogger<OutboxPollingService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public OutboxPollingService(
            ILogger<OutboxPollingService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox polling service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();

                    var reader = scope.ServiceProvider.GetRequiredService<IOutboxMessageReader>();
                    var stateRepository = scope.ServiceProvider.GetRequiredService<IOutboxMessageStateRepository>();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxEventDispatcher>();

                    var messages = await reader.SelectPendingAsync(
                        topN: 20,
                        nowUtc: DateTime.UtcNow,
                        cancellationToken: stoppingToken);

                    foreach (var message in messages)
                    {
                        await ProcessMessageAsync(message, stateRepository, dispatcher, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox polling loop failed.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessMessageAsync(
            OutboxMessageRecord message,
            IOutboxMessageStateRepository stateRepository,
            IOutboxEventDispatcher dispatcher,
            CancellationToken cancellationToken)
        {
            try
            {
                await stateRepository.MarkProcessingAsync(message.OutboxMessageId, cancellationToken);

                _logger.LogInformation(
                    "Processing outbox message. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, EventType={EventType}",
                    message.OutboxMessageId,
                    message.MessageId,
                    message.EventType);

                await dispatcher.DispatchAsync(message, cancellationToken);

                await stateRepository.MarkPublishedAsync(message.OutboxMessageId, cancellationToken);

                _logger.LogInformation(
                    "Published outbox message. OutboxMessageId={OutboxMessageId}, EventType={EventType}",
                    message.OutboxMessageId,
                    message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process outbox message. OutboxMessageId={OutboxMessageId}, EventType={EventType}",
                    message.OutboxMessageId,
                    message.EventType);

                await stateRepository.MarkFailedAsync(
                    message.OutboxMessageId,
                    nextRetryAt: DateTime.UtcNow.AddMinutes(1),
                    lastError: ex.Message,
                    cancellationToken: cancellationToken);
            }
        }
    }
}