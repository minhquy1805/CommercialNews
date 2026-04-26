using CommercialNews.BuildingBlocks.Outbox.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommercialNews.BuildingBlocks.Outbox.Runtime;

public abstract class BaseOutboxPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<OutboxWorkerOptions> _optionsMonitor;
    private readonly ILogger _logger;

    protected BaseOutboxPollingService(
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<OutboxWorkerOptions> optionsMonitor,
        ILogger logger)
    {
        _serviceScopeFactory = serviceScopeFactory
            ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _optionsMonitor = optionsMonitor
            ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    protected abstract string WorkerName { get; }

    protected abstract string OptionsName { get; }

    private OutboxWorkerOptions Options => _optionsMonitor.Get(OptionsName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.IsEnabled)
        {
            _logger.LogInformation("{WorkerName} outbox polling service is disabled.", WorkerName);
            return;
        }

        _logger.LogInformation(
            "{WorkerName} outbox polling service started. BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, ErrorDelaySeconds={ErrorDelaySeconds}",
            WorkerName,
            Options.BatchSize,
            Options.PollIntervalSeconds,
            Options.ErrorDelaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int claimedCount = await ProcessPendingOutboxMessagesAsync(stoppingToken);

                int delaySeconds = claimedCount > 0
                    ? 1
                    : Options.PollIntervalSeconds;

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception in {WorkerName} outbox polling service.",
                    WorkerName);

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(Options.ErrorDelaySeconds),
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("{WorkerName} outbox polling service stopped.", WorkerName);
    }

    private async Task<int> ProcessPendingOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();

        var batchProcessor = scope.ServiceProvider.GetRequiredService<IOutboxBatchProcessor>();

        var result = await batchProcessor.ProcessAsync(
            new ProcessPendingOutboxMessagesRequest
            {
                BatchSize = Options.BatchSize,
                StopOnFirstFailure = false
            },
            cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            _logger.LogWarning(
                "Failed to process pending {WorkerName} outbox messages. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, ErrorDetails={ErrorDetails}",
                WorkerName,
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
                "No pending {WorkerName} outbox messages found. RequestedBatchSize={RequestedBatchSize}",
                WorkerName,
                response.RequestedBatchSize);

            return 0;
        }

        _logger.LogInformation(
            "Processed pending {WorkerName} outbox messages. RequestedBatchSize={RequestedBatchSize}, ClaimedCount={ClaimedCount}, ProcessedCount={ProcessedCount}, SucceededCount={SucceededCount}, FailedCount={FailedCount}",
            WorkerName,
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
                    "{WorkerName} outbox message processing failed. OutboxMessageId={OutboxMessageId}, MessageId={MessageId}, EventType={EventType}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    WorkerName,
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