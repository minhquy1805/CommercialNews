using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Ports.Services;

namespace CommercialNews.Worker.Notifications.Processing;

public sealed class EmailDeliveryProcessingWorker : BackgroundService
{
    private readonly EmailDeliveryProcessingWorkerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailDeliveryProcessingWorker> _logger;

    public EmailDeliveryProcessingWorker(
        IOptions<EmailDeliveryProcessingWorkerOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailDeliveryProcessingWorker> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogInformation(
                "Email delivery processing worker is disabled.");

            return;
        }

        _logger.LogInformation(
            "Email delivery processing worker started. BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}",
            _options.BatchSize,
            _options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

                IEmailDeliveryProcessingService processingService =
                    scope.ServiceProvider.GetRequiredService<IEmailDeliveryProcessingService>();

                var result = await processingService.ProcessPendingAsync(
                    batchSize: _options.BatchSize,
                    cancellationToken: stoppingToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Email delivery processing failed. ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                        result.Error?.Code,
                        result.Error?.Message);

                    await DelayAsync(
                        TimeSpan.FromSeconds(_options.ErrorDelaySeconds),
                        stoppingToken);

                    continue;
                }

                int processedCount = result.Value;

                if (processedCount > 0)
                {
                    _logger.LogInformation(
                        "Email delivery processing completed. ProcessedCount={ProcessedCount}",
                        processedCount);

                    await DelayAsync(
                        TimeSpan.FromSeconds(_options.BusyDelaySeconds),
                        stoppingToken);

                    continue;
                }

                await DelayAsync(
                    TimeSpan.FromSeconds(_options.PollIntervalSeconds),
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
                    "Unhandled exception in email delivery processing worker.");

                await DelayAsync(
                    TimeSpan.FromSeconds(_options.ErrorDelaySeconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Email delivery processing worker stopped.");
    }

    private static async Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay, cancellationToken);
    }
}
