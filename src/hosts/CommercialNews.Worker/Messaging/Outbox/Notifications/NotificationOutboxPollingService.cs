using CommercialNews.BuildingBlocks.Abstractions.Time;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.Ports.Persistence.Write;
using Notifications.Application.UseCases.Outbox.ProcessOutboxMessage;
using Notifications.Domain.Entities;

namespace CommercialNews.Worker.Messaging.Outbox.Notifications;

public sealed class NotificationOutboxPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NotificationOutboxPollingService> _logger;
    private readonly NotificationWorkerOptions _options;

    public NotificationOutboxPollingService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<NotificationOutboxPollingService> logger,
        IOptions<NotificationWorkerOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory
            ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogInformation(
                "Notification outbox polling service is disabled.");

            return;
        }

        _logger.LogInformation(
            "Notification outbox polling service started. BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, ErrorDelaySeconds={ErrorDelaySeconds}",
            _options.BatchSize,
            _options.PollIntervalSeconds,
            _options.ErrorDelaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int processedCount = await ProcessBatchAsync(stoppingToken);

                if (processedCount == 0)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while polling notification outbox messages.");

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.ErrorDelaySeconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Notification outbox polling service stopped.");
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IOutboxMessageRepository outboxMessageRepository =
            scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();

        IProcessOutboxMessageUseCase processOutboxMessageUseCase =
            scope.ServiceProvider.GetRequiredService<IProcessOutboxMessageUseCase>();

        IDateTimeProvider dateTimeProvider =
            scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        DateTime nowUtc = dateTimeProvider.UtcNow;

        IReadOnlyList<OutboxMessage> pendingMessages =
            await outboxMessageRepository.ClaimPendingAsync(
                _options.BatchSize,
                nowUtc,
                cancellationToken);

        if (pendingMessages.Count == 0)
        {
            _logger.LogDebug(
                "No pending notification outbox messages were found.");

            return 0;
        }

        _logger.LogInformation(
            "Claimed {Count} notification outbox message(s) for processing.",
            pendingMessages.Count);

        int processedCount = 0;

        foreach (OutboxMessage outboxMessage in pendingMessages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var result = await processOutboxMessageUseCase.ExecuteAsync(
                    new ProcessOutboxMessageRequest
                    {
                        OutboxMessageId = outboxMessage.OutboxMessageId
                    },
                    cancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to process notification outbox message. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                        outboxMessage.OutboxMessageId,
                        outboxMessage.MessageId,
                        result.Error?.Code,
                        result.Error?.Message);

                    continue;
                }

                _logger.LogInformation(
                    "Processed notification outbox message successfully. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, OutboxStatus={OutboxStatus}, EmailDeliveryId={EmailDeliveryId}, EmailDeliveryStatus={EmailDeliveryStatus}",
                    outboxMessage.OutboxMessageId,
                    outboxMessage.MessageId,
                    result.Value?.OutboxStatus,
                    result.Value?.EmailDeliveryId,
                    result.Value?.EmailDeliveryStatus);

                processedCount++;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while processing notification outbox message. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}",
                    outboxMessage.OutboxMessageId,
                    outboxMessage.MessageId);
            }
        }

        return processedCount;
    }
}