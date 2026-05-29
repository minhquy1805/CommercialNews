using System.Text.Json;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.Extensions.Options;
using Notifications.Application.Configuration;
using Notifications.Application.Consumers.Identity.Payloads;
using Notifications.Application.Contracts.Ingestion;
using Notifications.Application.Ports.Services;
using Notifications.Domain.Enums;

namespace Notifications.Application.Consumers.Identity;

public sealed class IdentityEmailEventIngestionService : IIdentityEmailEventIngestionService
{
    private const string SourceModule = "Identity";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly INotificationIngestionService _notificationIngestionService;
    private readonly EmailDeliveryOptions _options;

    public IdentityEmailEventIngestionService(
        INotificationIngestionService notificationIngestionService,
        IOptions<EmailDeliveryOptions> options)
    {
        _notificationIngestionService = notificationIngestionService
            ?? throw new ArgumentNullException(nameof(notificationIngestionService));

        _options = options?.Value
            ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<Result<NotificationIngestionResult>> IngestVerificationEmailRequestedAsync(
        string messageId,
        string? correlationId,
        IdentityVerificationEmailRequestedPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string verificationLink = BuildUrlFromTemplate(
            _options.VerificationEmailUrlTemplate,
            payload.RawVerificationToken);

        string variablesJson = BuildVariablesJson(new Dictionary<string, string?>
        {
            ["fullName"] = payload.FullName,
            ["userPublicId"] = payload.UserPublicId,
            ["expiresAtUtc"] = payload.ExpiresAtUtc.ToString("O"),
            ["verificationTokenId"] = payload.VerificationTokenId.ToString(),
            ["verificationToken"] = payload.RawVerificationToken,
            ["verificationLink"] = verificationLink
        });

        return _notificationIngestionService.IngestEmailAsync(
            new EmailNotificationIngestionRequest
            {
                MessageId = messageId,
                BusinessDedupeKey = payload.BusinessDedupeKey,
                RecipientUserId = payload.UserId,
                ToEmail = payload.Email,
                TemplateKey = NotificationTemplateKey.VerifyEmail,
                VariablesJson = variablesJson,
                Provider = _options.Provider,
                Priority = _options.VerificationEmailPriority,
                CorrelationId = correlationId,
                SourceModule = SourceModule,
                SourceEventType = "identity.verification_email_requested",
                OccurredAtUtc = null
            },
            cancellationToken);
    }

    public Task<Result<NotificationIngestionResult>> IngestPasswordResetRequestedAsync(
        string messageId,
        string? correlationId,
        IdentityPasswordResetRequestedPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string resetPasswordLink = BuildUrlFromTemplate(
            _options.ResetPasswordUrlTemplate,
            payload.RawResetToken);

        string variablesJson = BuildVariablesJson(new Dictionary<string, string?>
        {
            ["fullName"] = payload.FullName,
            ["userPublicId"] = payload.UserPublicId,
            ["expiresAtUtc"] = payload.ExpiresAtUtc.ToString("O"),
            ["resetTokenId"] = payload.ResetTokenId.ToString(),
            ["resetToken"] = payload.RawResetToken,
            ["resetPasswordLink"] = resetPasswordLink
        });

        return _notificationIngestionService.IngestEmailAsync(
            new EmailNotificationIngestionRequest
            {
                MessageId = messageId,
                BusinessDedupeKey = payload.BusinessDedupeKey,
                RecipientUserId = payload.UserId,
                ToEmail = payload.Email,
                TemplateKey = NotificationTemplateKey.ResetPassword,
                VariablesJson = variablesJson,
                Provider = _options.Provider,
                Priority = _options.PasswordResetPriority,
                CorrelationId = correlationId,
                SourceModule = SourceModule,
                SourceEventType = "identity.password_reset_requested",
                OccurredAtUtc = null
            },
            cancellationToken);
    }

    public Task<Result<NotificationIngestionResult>> IngestPasswordChangedAsync(
        string messageId,
        string? correlationId,
        IdentityPasswordChangedPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string variablesJson = BuildVariablesJson(new Dictionary<string, string?>
        {
            ["fullName"] = payload.FullName,
            ["userPublicId"] = payload.UserPublicId,
            ["reason"] = payload.Reason,
            ["changedAtUtc"] = payload.ChangedAtUtc.ToString("O")
        });

        return _notificationIngestionService.IngestEmailAsync(
            new EmailNotificationIngestionRequest
            {
                MessageId = messageId,
                BusinessDedupeKey = payload.BusinessDedupeKey,
                RecipientUserId = payload.UserId,
                ToEmail = payload.Email,
                TemplateKey = NotificationTemplateKey.PasswordChanged,
                VariablesJson = variablesJson,
                Provider = _options.Provider,
                Priority = _options.PasswordChangedPriority,
                CorrelationId = correlationId,
                SourceModule = SourceModule,
                SourceEventType = "identity.password_changed",
                OccurredAtUtc = payload.ChangedAtUtc
            },
            cancellationToken);
    }

    public Task<Result<NotificationIngestionResult>> IngestEmailVerifiedAsync(
        string messageId,
        string? correlationId,
        IdentityEmailVerifiedPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string variablesJson = BuildVariablesJson(new Dictionary<string, string?>
        {
            ["fullName"] = payload.FullName,
            ["userPublicId"] = payload.UserPublicId,
            ["verificationTokenId"] = payload.VerificationTokenId.ToString(),
            ["verifiedAtUtc"] = payload.VerifiedAtUtc.ToString("O")
        });

        return _notificationIngestionService.IngestEmailAsync(
            new EmailNotificationIngestionRequest
            {
                MessageId = messageId,
                BusinessDedupeKey = payload.BusinessDedupeKey,
                RecipientUserId = payload.UserId,
                ToEmail = payload.Email,
                TemplateKey = NotificationTemplateKey.EmailVerified,
                VariablesJson = variablesJson,
                Provider = _options.Provider,
                Priority = _options.EmailVerifiedPriority,
                CorrelationId = correlationId,
                SourceModule = SourceModule,
                SourceEventType = "identity.email_verified",
                OccurredAtUtc = payload.VerifiedAtUtc
            },
            cancellationToken);
    }

    private static string BuildVariablesJson(
        IReadOnlyDictionary<string, string?> variables)
    {
        Dictionary<string, string> normalizedVariables = variables
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value!,
                StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(normalizedVariables, JsonOptions);
    }

    private static string BuildUrlFromTemplate(
        string urlTemplate,
        string rawToken)
    {
        if (string.IsNullOrWhiteSpace(urlTemplate))
        {
            throw new ArgumentException("URL template is required.", nameof(urlTemplate));
        }

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Raw token is required.", nameof(rawToken));
        }

        return urlTemplate.Replace(
            "{token}",
            Uri.EscapeDataString(rawToken.Trim()),
            StringComparison.OrdinalIgnoreCase);
    }
}
