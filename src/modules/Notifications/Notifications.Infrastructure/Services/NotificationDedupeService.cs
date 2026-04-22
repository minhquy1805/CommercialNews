using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Services;

public sealed class NotificationDedupeService : INotificationDedupeService
{
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;

    public NotificationDedupeService(IEmailDeliveryRepository emailDeliveryRepository)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
    }

    public async Task<DedupeCheckResult> CheckAsync(
        NotificationDedupeCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string messageId = request.MessageId?.Trim()
            ?? throw new ArgumentException("MessageId is required.", nameof(request));

        string businessDedupeKey = request.BusinessDedupeKey?.Trim()
            ?? throw new ArgumentException("BusinessDedupeKey is required.", nameof(request));

        if (messageId.Length == 0)
        {
            throw new ArgumentException("MessageId is required.", nameof(request));
        }

        if (businessDedupeKey.Length == 0)
        {
            throw new ArgumentException("BusinessDedupeKey is required.", nameof(request));
        }

        var existingByMessageId = await _emailDeliveryRepository.GetByMessageIdAsync(
            messageId,
            cancellationToken);

        if (existingByMessageId is not null)
        {
            return new DedupeCheckResult
            {
                IsDuplicateMessage = true,
                IsDuplicateBusinessIntent = false,
                ShouldSuppress = true,
                ExistingEmailDeliveryId = existingByMessageId.EmailDeliveryId,
                ExistingStatus = existingByMessageId.Status,
                Reason = "A delivery already exists for the same message id."
            };
        }

        var existingByBusinessKey = await _emailDeliveryRepository.GetByBusinessDedupeKeyAsync(
            businessDedupeKey,
            cancellationToken);

        if (existingByBusinessKey is null)
        {
            return new DedupeCheckResult
            {
                IsDuplicateMessage = false,
                IsDuplicateBusinessIntent = false,
                ShouldSuppress = false,
                ExistingEmailDeliveryId = null,
                ExistingStatus = null,
                Reason = null
            };
        }

        return new DedupeCheckResult
        {
            IsDuplicateMessage = false,
            IsDuplicateBusinessIntent = true,
            ShouldSuppress = true,
            ExistingEmailDeliveryId = existingByBusinessKey.EmailDeliveryId,
            ExistingStatus = existingByBusinessKey.Status,
            Reason = BuildBusinessIntentReason(existingByBusinessKey.Status)
        };
    }

    private static string BuildBusinessIntentReason(string status)
    {
        if (string.Equals(status, EmailDeliveryStatus.Sent, StringComparison.OrdinalIgnoreCase))
        {
            return "A delivery for the same business intent has already been sent.";
        }

        if (string.Equals(status, EmailDeliveryStatus.Suppressed, StringComparison.OrdinalIgnoreCase))
        {
            return "A delivery for the same business intent was already suppressed.";
        }

        if (string.Equals(status, EmailDeliveryStatus.Queued, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, EmailDeliveryStatus.Sending, StringComparison.OrdinalIgnoreCase))
        {
            return "A delivery for the same business intent is already in progress.";
        }

        if (string.Equals(status, EmailDeliveryStatus.Ambiguous, StringComparison.OrdinalIgnoreCase))
        {
            return "A delivery for the same business intent already exists with an ambiguous outcome.";
        }

        if (string.Equals(status, EmailDeliveryStatus.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return "A delivery for the same business intent already exists in failed state.";
        }

        if (string.Equals(status, EmailDeliveryStatus.Dead, StringComparison.OrdinalIgnoreCase))
        {
            return "A delivery for the same business intent already exists in dead state.";
        }

        return "A delivery for the same business intent already exists.";
    }
}