using Microsoft.Extensions.Options;
using Notifications.Application.Contracts.Outbox.Requests;
using Notifications.Application.UseCases.Outbox.ProcessPendingOutboxMessages;

namespace CommercialNews.Worker.Notifications;

public sealed class OutboxPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly NotificationWorkerOptions _options;
    private readonly ILogger<OutboxPollingService> _logger;

    public OutboxPollingService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<NotificationWorkerOptions> options,
        ILogger<OutboxPollingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory
            ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _options = options?.Value
            ?? throw new ArgumentNullException(nameof(options));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogInformation("Outbox polling service is disabled.");
            return;
        }

        _logger.LogInformation(
            "Outbox polling service started. BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, ErrorDelaySeconds={ErrorDelaySeconds}",
            _options.BatchSize,
            _options.PollIntervalSeconds,
            _options.ErrorDelaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int claimedCount = await ProcessPendingOutboxMessagesAsync(stoppingToken);

                int delaySeconds = claimedCount > 0
                    ? 1
                    : _options.PollIntervalSeconds;

                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception in outbox polling service.");

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.ErrorDelaySeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Outbox polling service stopped.");
    }

    private async Task<int> ProcessPendingOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();

        var useCase = scope.ServiceProvider.GetRequiredService<IProcessPendingOutboxMessagesUseCase>();

        var result = await useCase.ExecuteAsync(
            new ProcessPendingOutboxMessagesRequest
            {
                BatchSize = _options.BatchSize,
                StopOnFirstFailure = false
            },
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            _logger.LogWarning(
                "Failed to process pending outbox messages. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, ErrorDetails={ErrorDetails}",
                error.Code,
                error.Message,
                error.Details is null
                    ? null
                    : string.Join(" | ", error.Details));

            return 0;
        }

        var response = result.Value!;

        if (response.ClaimedCount == 0)
        {
            _logger.LogDebug(
                "No pending outbox messages found. RequestedBatchSize={RequestedBatchSize}",
                response.RequestedBatchSize);

            return 0;
        }

        _logger.LogInformation(
            "Processed pending outbox messages. RequestedBatchSize={RequestedBatchSize}, ClaimedCount={ClaimedCount}, ProcessedCount={ProcessedCount}, SucceededCount={SucceededCount}, FailedCount={FailedCount}",
            response.RequestedBatchSize,
            response.ClaimedCount,
            response.ProcessedCount,
            response.SucceededCount,
            response.FailedCount);

        if (response.FailedCount > 0)
        {
            foreach (var item in response.Items.Where(x => !x.Succeeded))
            {
                _logger.LogWarning(
                    "Outbox message processing failed. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    item.OutboxMessageId,
                    item.MessageId,
                    item.EventType,
                    item.ErrorCode,
                    item.ErrorMessage);
            }
        }

        return response.ClaimedCount;
    }
}