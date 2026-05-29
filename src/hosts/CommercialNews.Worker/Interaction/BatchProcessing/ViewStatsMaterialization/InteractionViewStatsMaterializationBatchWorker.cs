using System.Diagnostics;
using Interaction.Application.UseCases.ArticleInteractionStats.ProcessPendingViewStatsMaterialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommercialNews.Worker.Interaction.BatchProcessing.ViewStatsMaterialization;

public sealed class InteractionViewStatsMaterializationBatchWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<InteractionViewStatsMaterializationBatchOptions> _optionsMonitor;
    private readonly ILogger<InteractionViewStatsMaterializationBatchWorker> _logger;

    public InteractionViewStatsMaterializationBatchWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<InteractionViewStatsMaterializationBatchOptions> optionsMonitor,
        ILogger<InteractionViewStatsMaterializationBatchWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory
            ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        _optionsMonitor = optionsMonitor
            ?? throw new ArgumentNullException(nameof(optionsMonitor));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    private InteractionViewStatsMaterializationBatchOptions Options =>
        _optionsMonitor.CurrentValue;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        InteractionViewStatsMaterializationBatchOptions startupOptions = Options;

        if (!startupOptions.IsEnabled)
        {
            _logger.LogInformation(
                "Interaction view stats materialization batch worker is disabled.");

            return;
        }

        _logger.LogInformation(
            "Interaction view stats materialization batch worker started. BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, BusyDelaySeconds={BusyDelaySeconds}, ErrorDelaySeconds={ErrorDelaySeconds}",
            startupOptions.BatchSize,
            startupOptions.PollIntervalSeconds,
            startupOptions.BusyDelaySeconds,
            startupOptions.ErrorDelaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            InteractionViewStatsMaterializationBatchOptions options = Options;

            if (!options.IsEnabled)
            {
                _logger.LogInformation(
                    "Interaction view stats materialization batch worker was disabled by configuration.");

                await DelaySafelyAsync(
                    options.PollIntervalSeconds,
                    stoppingToken);

                continue;
            }

            try
            {
                int selectedCount =
                    await ProcessPendingViewStatsMaterializationBatchAsync(
                        options,
                        stoppingToken);

                int delaySeconds = selectedCount > 0
                    ? options.BusyDelaySeconds
                    : options.PollIntervalSeconds;

                await DelaySafelyAsync(
                    delaySeconds,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception in interaction view stats materialization batch worker.");

                await DelaySafelyAsync(
                    options.ErrorDelaySeconds,
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Interaction view stats materialization batch worker stopped.");
    }

    private async Task<int> ProcessPendingViewStatsMaterializationBatchAsync(
        InteractionViewStatsMaterializationBatchOptions options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await using AsyncServiceScope scope =
            _serviceScopeFactory.CreateAsyncScope();

        var useCase =
            scope.ServiceProvider.GetRequiredService<
                IProcessPendingViewStatsMaterializationUseCase>();

        var result = await useCase.ExecuteAsync(
            options.BatchSize,
            cancellationToken);

        stopwatch.Stop();

        if (result.IsFailure)
        {
            var error = result.Error!;

            _logger.LogWarning(
                "Failed to process pending interaction view stats materialization batch. RequestedBatchSize={RequestedBatchSize}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, ErrorDetails={ErrorDetails}, ElapsedMilliseconds={ElapsedMilliseconds}",
                options.BatchSize,
                error.Code,
                error.Message,
                error.Details is null
                    ? null
                    : string.Join(" | ", error.Details),
                stopwatch.ElapsedMilliseconds);

            return 0;
        }

        var response = result.Value!;

        if (response.SelectedCount == 0)
        {
            _logger.LogDebug(
                "No pending interaction view stats materialization work found. RequestedBatchSize={RequestedBatchSize}, ElapsedMilliseconds={ElapsedMilliseconds}",
                options.BatchSize,
                stopwatch.ElapsedMilliseconds);

            return 0;
        }

        _logger.LogInformation(
            "Processed pending interaction view stats materialization batch. RequestedBatchSize={RequestedBatchSize}, SelectedCount={SelectedCount}, MaterializedCount={MaterializedCount}, UnchangedCount={UnchangedCount}, FailedCount={FailedCount}, ElapsedMilliseconds={ElapsedMilliseconds}",
            options.BatchSize,
            response.SelectedCount,
            response.MaterializedCount,
            response.UnchangedCount,
            response.FailedCount,
            stopwatch.ElapsedMilliseconds);

        if (response.FailedCount > 0)
        {
            _logger.LogWarning(
                "Some interaction view stats materialization items failed and remain eligible for a later retry cycle. FailedCount={FailedCount}",
                response.FailedCount);
        }

        return response.SelectedCount;
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