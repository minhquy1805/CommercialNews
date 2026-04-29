using System.Text.Json;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Microsoft.Extensions.Options;
using Notifications.Application.Configuration;
using Notifications.Application.Consumers.Identity.Payloads;
using Notifications.Application.Errors;
using Notifications.Application.Ports.Persistence;
using Notifications.Application.Ports.Transactions;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using Notifications.Domain.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Notifications.Application.Consumers.Identity;

public sealed class IdentityEmailEventIngestionService : IIdentityEmailEventIngestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly INotificationsUnitOfWork _unitOfWork;
    private readonly EmailDeliveryOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IdentityEmailEventIngestionService(
        IEmailDeliveryRepository emailDeliveryRepository,
        INotificationsUnitOfWork unitOfWork,
        IOptions<EmailDeliveryOptions> options,
        IDateTimeProvider dateTimeProvider)
    {
        _emailDeliveryRepository = emailDeliveryRepository
            ?? throw new ArgumentNullException(nameof(emailDeliveryRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _options = options?.Value
            ?? throw new ArgumentNullException(nameof(options));

        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public Task<Result<long>> IngestVerificationEmailRequestedAsync(
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

        return IngestAsync(
            messageId: messageId,
            businessDedupeKey: payload.BusinessDedupeKey,
            recipientUserId: payload.UserId,
            toEmail: payload.Email,
            templateKey: NotificationTemplateKey.VerifyEmail,
            variablesJson: variablesJson,
            provider: _options.Provider,
            priority: _options.VerificationEmailPriority,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    public Task<Result<long>> IngestPasswordResetRequestedAsync(
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

        return IngestAsync(
            messageId: messageId,
            businessDedupeKey: payload.BusinessDedupeKey,
            recipientUserId: payload.UserId,
            toEmail: payload.Email,
            templateKey: NotificationTemplateKey.ResetPassword,
            variablesJson: variablesJson,
            provider: _options.Provider,
            priority: _options.PasswordResetPriority,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    public Task<Result<long>> IngestPasswordChangedAsync(
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

        return IngestAsync(
            messageId: messageId,
            businessDedupeKey: payload.BusinessDedupeKey,
            recipientUserId: payload.UserId,
            toEmail: payload.Email,
            templateKey: NotificationTemplateKey.PasswordChanged,
            variablesJson: variablesJson,
            provider: _options.Provider,
            priority: _options.PasswordChangedPriority,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    public Task<Result<long>> IngestEmailVerifiedAsync(
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

        return IngestAsync(
            messageId: messageId,
            businessDedupeKey: payload.BusinessDedupeKey,
            recipientUserId: payload.UserId,
            toEmail: payload.Email,
            templateKey: NotificationTemplateKey.EmailVerified,
            variablesJson: variablesJson,
            provider: _options.Provider,
            priority: _options.EmailVerifiedPriority,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    private async Task<Result<long>> IngestAsync(
        string messageId,
        string businessDedupeKey,
        long? recipientUserId,
        string toEmail,
        string templateKey,
        string variablesJson,
        string provider,
        byte priority,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return Result<long>.Failure(NotificationsErrors.ValidationFailed);
        }

        if (string.IsNullOrWhiteSpace(businessDedupeKey))
        {
            return Result<long>.Failure(NotificationsErrors.ValidationFailed);
        }

        try
        {
            string normalizedMessageId = messageId.Trim();
            string normalizedBusinessDedupeKey = businessDedupeKey.Trim();
            string normalizedToEmail = toEmail.Trim();
            string? normalizedCorrelationId = NormalizeOptional(correlationId);
            DateTime nowUtc = _dateTimeProvider.UtcNow;

            EmailDelivery? existingByMessageId =
                await _emailDeliveryRepository.GetByMessageIdAsync(
                    normalizedMessageId,
                    cancellationToken);

            if (existingByMessageId is not null)
            {
                return Result<long>.Success(existingByMessageId.EmailDeliveryId);
            }

            EmailDelivery? existingByBusinessDedupeKey =
                await _emailDeliveryRepository.GetByBusinessDedupeKeyAsync(
                    normalizedBusinessDedupeKey,
                    cancellationToken);

            if (existingByBusinessDedupeKey is not null)
            {
                return Result<long>.Success(existingByBusinessDedupeKey.EmailDeliveryId);
            }

            EmailDelivery emailDelivery = EmailDelivery.Create(
                messageId: normalizedMessageId,
                businessDedupeKey: normalizedBusinessDedupeKey,
                toEmail: normalizedToEmail,
                templateKey: templateKey,
                variablesJson: variablesJson,
                provider: provider,
                priority: priority,
                nowUtc: nowUtc,
                recipientUserId: recipientUserId,
                correlationId: normalizedCorrelationId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                long emailDeliveryId = await _emailDeliveryRepository.InsertAsync(
                    emailDelivery,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<long>.Success(emailDeliveryId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException)
        {
            return Result<long>.Failure(NotificationsErrors.DependencyUnavailable);
        }
        catch (NotificationsDomainException)
        {
            return Result<long>.Failure(NotificationsErrors.ValidationFailed);
        }
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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