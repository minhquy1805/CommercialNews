using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Persistence.Write;
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

        if (string.IsNullOrWhiteSpace(request.MessageId))
        {
            return new DedupeCheckResult
            {
                IsDuplicateMessage = false,
                IsDuplicateBusinessIntent = false,
                ShouldSuppress = false,
                Reason = "Message id is missing."
            };
        }

        if (string.IsNullOrWhiteSpace(request.BusinessDedupeKey))
        {
            return new DedupeCheckResult
            {
                IsDuplicateMessage = false,
                IsDuplicateBusinessIntent = false,
                ShouldSuppress = false,
                Reason = "Business dedupe key is missing."
            };
        }

        string messageId = request.MessageId.Trim();
        string businessDedupeKey = request.BusinessDedupeKey.Trim();

        // Important:
        // Message-level dedupe protects against re-processing the exact same
        // technical message more than once.
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
                Reason = "A delivery already exists for the same message id."
            };
        }

        // Important:
        // Business-intent dedupe protects against duplicate notification sends
        // for the same underlying business action, even if the technical message id changes.
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
                Reason = null
            };
        }

        // Important:
        // If the business intent already produced a delivery that is still meaningful
        // (queued, sending, sent, ambiguous, failed, dead, or suppressed),
        // phase 1 chooses to suppress creating a new duplicate delivery.
        return new DedupeCheckResult
        {
            IsDuplicateMessage = false,
            IsDuplicateBusinessIntent = true,
            ShouldSuppress = true,
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