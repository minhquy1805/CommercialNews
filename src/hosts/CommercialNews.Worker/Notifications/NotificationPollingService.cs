using Microsoft.Extensions.Options;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.UseCases.EmailDeliveries.ProcessPendingEmailDeliveries;

namespace CommercialNews.Worker.Notifications;

public sealed class NotificationPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NotificationPollingService> _logger;
    private readonly NotificationWorkerOptions _options;

    public NotificationPollingService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<NotificationPollingService> logger,
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
                "Notification polling service is disabled.");

            return;
        }

        _logger.LogInformation(
            "Notification polling service started. BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, ErrorDelaySeconds={ErrorDelaySeconds}",
            _options.BatchSize,
            _options.PollIntervalSeconds,
            _options.ErrorDelaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int claimedCount = await ProcessBatchAsync(stoppingToken);

                if (claimedCount == 0)
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
                    "An unexpected error occurred while polling pending notification deliveries.");

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.ErrorDelaySeconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Notification polling service stopped.");
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();

        IProcessPendingEmailDeliveriesUseCase useCase =
            scope.ServiceProvider.GetRequiredService<IProcessPendingEmailDeliveriesUseCase>();

        var result = await useCase.ExecuteAsync(
            new ProcessPendingEmailDeliveriesRequest
            {
                TopN = _options.BatchSize
            },
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to process pending notification deliveries. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, ErrorDetails={ErrorDetails}",
                result.Error?.Code,
                result.Error?.Message,
                result.Error?.Details is null
                    ? null
                    : string.Join(" | ", result.Error.Details));

            return 0;
        }

        ProcessPendingEmailDeliveriesResponse response = result.Value!;

        if (response.ClaimedCount == 0)
        {
            _logger.LogDebug(
                "No pending notification deliveries were found.");

            return 0;
        }

        _logger.LogInformation(
            "Processed notification delivery batch. Claimed={ClaimedCount}, Processed={ProcessedCount}, Succeeded={SucceededCount}, Failed={FailedCount}, Ambiguous={AmbiguousCount}, Dead={DeadCount}, Suppressed={SuppressedCount}",
            response.ClaimedCount,
            response.ProcessedCount,
            response.SucceededCount,
            response.FailedCount,
            response.AmbiguousCount,
            response.DeadCount,
            response.SuppressedCount);

        return response.ClaimedCount;
    }
}