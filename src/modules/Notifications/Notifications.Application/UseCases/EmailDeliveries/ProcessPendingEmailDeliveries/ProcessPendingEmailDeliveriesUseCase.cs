using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Notifications.Application.Contracts.EmailDeliveries.Requests;
using Notifications.Application.Contracts.EmailDeliveries.Responses;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.UseCases.EmailDeliveries.ProcessEmailDelivery;
using Notifications.Application.Validation.EmailDeliveries.ProcessPendingEmailDeliveries;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;

namespace Notifications.Application.UseCases.EmailDeliveries.ProcessPendingEmailDeliveries;

/// <summary>
/// Claims a batch of pending email deliveries and processes each item independently.
/// This use case does not wrap the whole batch in a single transaction.
/// Each delivery is delegated to IProcessEmailDeliveryUseCase, which owns
/// per-delivery transactional consistency.
/// </summary>
public sealed class ProcessPendingEmailDeliveriesUseCase : IProcessPendingEmailDeliveriesUseCase
{
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly IProcessEmailDeliveryUseCase _processEmailDeliveryUseCase;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProcessPendingEmailDeliveriesUseCase(
        IEmailDeliveryRepository emailDeliveryRepository,
        IProcessEmailDeliveryUseCase processEmailDeliveryUseCase,
        IDateTimeProvider dateTimeProvider)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
        _processEmailDeliveryUseCase = processEmailDeliveryUseCase
            ?? throw new ArgumentNullException(nameof(processEmailDeliveryUseCase));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<ProcessPendingEmailDeliveriesResponse>> ExecuteAsync(
        ProcessPendingEmailDeliveriesRequest request,
        CancellationToken cancellationToken = default)
    {
        Error? validationError = ProcessPendingEmailDeliveriesValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ProcessPendingEmailDeliveriesResponse>.Failure(validationError);
        }

        try
        {
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            IReadOnlyList<EmailDelivery> claimedDeliveries =
                await _emailDeliveryRepository.ClaimPendingAsync(
                    request.TopN,
                    nowUtc,
                    cancellationToken);

            var response = new ProcessPendingEmailDeliveriesResponse
            {
                ClaimedCount = claimedDeliveries.Count
            };

            int processedCount = 0;
            int succeededCount = 0;
            int failedCount = 0;
            int ambiguousCount = 0;
            int deadCount = 0;
            int suppressedCount = 0;

            foreach (EmailDelivery delivery in claimedDeliveries)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                Result<ProcessEmailDeliveryResponse> processResult;

                try
                {
                    processResult = await _processEmailDeliveryUseCase.ExecuteAsync(
                        new ProcessEmailDeliveryRequest
                        {
                            EmailDeliveryId = delivery.EmailDeliveryId,
                            CorrelationId = delivery.CorrelationId
                        },
                        cancellationToken);
                }
                catch
                {
                    failedCount++;
                    processedCount++;
                    continue;
                }

                processedCount++;

                if (processResult.IsFailure)
                {
                    failedCount++;
                    continue;
                }

                ProcessEmailDeliveryResponse value = processResult.Value!;

                if (value.IsSuccess)
                {
                    succeededCount++;
                    continue;
                }

                if (value.IsAmbiguous ||
                    string.Equals(value.Status, EmailDeliveryStatus.Ambiguous, StringComparison.OrdinalIgnoreCase))
                {
                    ambiguousCount++;
                    continue;
                }

                if (string.Equals(value.Status, EmailDeliveryStatus.Dead, StringComparison.OrdinalIgnoreCase))
                {
                    deadCount++;
                    continue;
                }

                if (string.Equals(value.Status, EmailDeliveryStatus.Suppressed, StringComparison.OrdinalIgnoreCase))
                {
                    suppressedCount++;
                    continue;
                }

                failedCount++;
            }

            return Result<ProcessPendingEmailDeliveriesResponse>.Success(
                new ProcessPendingEmailDeliveriesResponse
                {
                    ClaimedCount = claimedDeliveries.Count,
                    ProcessedCount = processedCount,
                    SucceededCount = succeededCount,
                    FailedCount = failedCount,
                    AmbiguousCount = ambiguousCount,
                    DeadCount = deadCount,
                    SuppressedCount = suppressedCount
                });
        }
        catch (PersistenceException exception)
        {
            return Result<ProcessPendingEmailDeliveriesResponse>.Failure(
                NotificationsErrors.DependencyUnavailable with
                {
                    Details = new[]
                    {
                        $"persistence_code: {exception.Code}",
                        $"persistence_message: {exception.Message}"
                    }
                });
        }
    }
}