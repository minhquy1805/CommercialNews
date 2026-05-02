using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Notifications.Application.Contracts.Ingestion;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Transactions;
using Notifications.Domain.Entities;
using Notifications.Domain.Exceptions;

namespace Notifications.Application.Services;

public sealed class NotificationIngestionService : INotificationIngestionService
{
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationIngestionService(
        IEmailDeliveryRepository emailDeliveryRepository,
        INotificationsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<NotificationIngestionResult>> IngestEmailAsync(
        EmailNotificationIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.MessageId) ||
            string.IsNullOrWhiteSpace(request.BusinessDedupeKey) ||
            string.IsNullOrWhiteSpace(request.ToEmail) ||
            string.IsNullOrWhiteSpace(request.TemplateKey) ||
            string.IsNullOrWhiteSpace(request.VariablesJson) ||
            string.IsNullOrWhiteSpace(request.Provider))
        {
            return Result<NotificationIngestionResult>.Failure(
                NotificationsErrors.ValidationFailed);
        }

        try
        {
            string normalizedMessageId = request.MessageId.Trim();
            string normalizedBusinessDedupeKey = request.BusinessDedupeKey.Trim();
            string normalizedToEmail = request.ToEmail.Trim();
            string normalizedTemplateKey = request.TemplateKey.Trim();
            string normalizedProvider = request.Provider.Trim();
            string? normalizedCorrelationId = NormalizeOptional(request.CorrelationId);

            EmailDelivery? existingByMessageId =
                await _emailDeliveryRepository.GetByMessageIdAsync(
                    normalizedMessageId,
                    cancellationToken);

            if (existingByMessageId is not null)
            {
                return Result<NotificationIngestionResult>.Success(
                    NotificationIngestionResult.Deduped(
                        existingByMessageId.EmailDeliveryId,
                        normalizedMessageId,
                        normalizedBusinessDedupeKey));
            }

            EmailDelivery? existingByBusinessDedupeKey =
                await _emailDeliveryRepository.GetByBusinessDedupeKeyAsync(
                    normalizedBusinessDedupeKey,
                    cancellationToken);

            if (existingByBusinessDedupeKey is not null)
            {
                return Result<NotificationIngestionResult>.Success(
                    NotificationIngestionResult.Deduped(
                        existingByBusinessDedupeKey.EmailDeliveryId,
                        normalizedMessageId,
                        normalizedBusinessDedupeKey));
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;

            EmailDelivery emailDelivery = EmailDelivery.Create(
                messageId: normalizedMessageId,
                businessDedupeKey: normalizedBusinessDedupeKey,
                toEmail: normalizedToEmail,
                templateKey: normalizedTemplateKey,
                variablesJson: request.VariablesJson,
                provider: normalizedProvider,
                priority: request.Priority,
                nowUtc: nowUtc,
                recipientUserId: request.RecipientUserId,
                correlationId: normalizedCorrelationId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                long emailDeliveryId = await _emailDeliveryRepository.InsertAsync(
                    emailDelivery,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<NotificationIngestionResult>.Success(
                    NotificationIngestionResult.Inserted(
                        emailDeliveryId,
                        normalizedMessageId,
                        normalizedBusinessDedupeKey));
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<NotificationIngestionResult>.Failure(
                NotificationsErrors.DependencyUnavailable);
        }
        catch (NotificationsDomainException)
        {
            return Result<NotificationIngestionResult>.Failure(
                NotificationsErrors.ValidationFailed);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}