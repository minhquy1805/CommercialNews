using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using CommercialNews.BuildingBlocks.Outbox.Runtime;
using CommercialNews.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace CommercialNews.Worker.Outbox;

public sealed class OutboxPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<OutboxWorkerOptions> _optionsMonitor;
    private readonly ILogger<OutboxPollingService> _logger;

    public OutboxPollingService(
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<OutboxWorkerOptions> optionsMonitor,
        ILogger<OutboxPollingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory
            ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        _optionsMonitor = optionsMonitor
            ?? throw new ArgumentNullException(nameof(optionsMonitor));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    private OutboxWorkerOptions Options => _optionsMonitor.CurrentValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OutboxWorkerOptions startupOptions = Options;

        if (!startupOptions.IsEnabled)
        {
            _logger.LogInformation("Outbox polling service is disabled.");
            return;
        }

        _logger.LogInformation(
            "Outbox polling service started. BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, BusyDelaySeconds={BusyDelaySeconds}, ErrorDelaySeconds={ErrorDelaySeconds}, StopOnFirstFailure={StopOnFirstFailure}, MaxRetryAttempts={MaxRetryAttempts}",
            startupOptions.BatchSize,
            startupOptions.PollIntervalSeconds,
            startupOptions.BusyDelaySeconds,
            startupOptions.ErrorDelaySeconds,
            startupOptions.StopOnFirstFailure,
            startupOptions.MaxRetryAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            OutboxWorkerOptions options = Options;

            if (!options.IsEnabled)
            {
                _logger.LogInformation("Outbox polling service was disabled by configuration.");

                await DelaySafelyAsync(
                    options.PollIntervalSeconds,
                    stoppingToken);

                continue;
            }

            try
            {
                int claimedCount = await ProcessPendingOutboxMessagesAsync(
                    options,
                    stoppingToken);

                int delaySeconds = claimedCount > 0
                    ? options.BusyDelaySeconds
                    : options.PollIntervalSeconds;

                await DelaySafelyAsync(
                    delaySeconds,
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

                await DelaySafelyAsync(
                    options.ErrorDelaySeconds,
                    stoppingToken);
            }
        }

        _logger.LogInformation("Outbox polling service stopped.");
    }

    private async Task<int> ProcessPendingOutboxMessagesAsync(
        OutboxWorkerOptions options,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope =
            _serviceScopeFactory.CreateAsyncScope();

        var batchProcessor =
            scope.ServiceProvider.GetRequiredService<IOutboxBatchProcessor>();

        var result = await batchProcessor.ProcessAsync(
            new ProcessPendingOutboxMessagesRequest
            {
                BatchSize = options.BatchSize,
                StopOnFirstFailure = options.StopOnFirstFailure
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

    private static async Task DelaySafelyAsync(
        int delaySeconds,
        CancellationToken cancellationToken)
    {
        int safeDelaySeconds = Math.Max(1, delaySeconds);

        await Task.Delay(
            TimeSpan.FromSeconds(safeDelaySeconds),
            cancellationToken);
    }
}