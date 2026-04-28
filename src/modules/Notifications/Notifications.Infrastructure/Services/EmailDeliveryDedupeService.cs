using Notifications.Application.Contracts.Services;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Infrastructure.Services;

public sealed class EmailDeliveryDedupeService : IEmailDeliveryDedupeService
{
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;

    public EmailDeliveryDedupeService(IEmailDeliveryRepository emailDeliveryRepository)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));
    }

    public async Task<EmailDeliveryDedupeCheckResult> CheckAsync(
        EmailDeliveryDedupeCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string messageId = NormalizeRequired(request.MessageId, nameof(request.MessageId));
        string businessDedupeKey = NormalizeRequired(
            request.BusinessDedupeKey,
            nameof(request.BusinessDedupeKey));

        var existingByMessageId = await _emailDeliveryRepository.GetByMessageIdAsync(
            messageId,
            cancellationToken);

        if (existingByMessageId is not null)
        {
            return new EmailDeliveryDedupeCheckResult
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
            return new EmailDeliveryDedupeCheckResult
            {
                IsDuplicateMessage = false,
                IsDuplicateBusinessIntent = false,
                ShouldSuppress = false
            };
        }

        return new EmailDeliveryDedupeCheckResult
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

        if (string.Equals(status, EmailDeliveryStatus.Queued, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, EmailDeliveryStatus.Sending, StringComparison.OrdinalIgnoreCase))
        {
            return "A delivery for the same business intent is already in progress.";
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

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}