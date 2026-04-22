using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;

namespace Identity.Infrastructure.Services;

public sealed class IdentityOutboxWriter : IIdentityOutboxWriter
{
    private const string AggregateTypeUserAccount = "UserAccount";

    private const string VerificationEmailRequestedEventType =
        "Identity.VerificationEmailRequested";

    private const string PasswordChangedEmailRequestedEventType =
        "Identity.PasswordChangedEmailRequested";

    private const string PasswordResetRequestedEventType =
        "Identity.PasswordResetRequested";

    private const string DevVerifyEmailEndpoint =
        "http://localhost:5226/api/v1/identity/auth/verify-email";

    private const string DevResetPasswordEndpoint =
        "http://localhost:5226/api/v1/identity/auth/reset-password";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public IdentityOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator,
        IIdentityUnitOfWork unitOfWork)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));
        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task EnqueueVerificationEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string rawVerificationToken,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateUserEnvelope(userId, userPublicId, email, occurredAtUtc);

        if (string.IsNullOrWhiteSpace(rawVerificationToken))
        {
            throw new ArgumentException("Raw verification token is required.", nameof(rawVerificationToken));
        }

        string verificationUrl = BuildVerificationUrl(rawVerificationToken);

        string payload = JsonSerializer.Serialize(
            new
            {
                businessDedupeKey = $"identity:verify-email:{userId}:{rawVerificationToken}",
                recipientUserId = userId,
                toEmail = email.Trim(),
                templateKey = "VerifyEmail",
                templateVersion = 1,
                subject = "Verify your email address",
                provider = "smtp",
                correlationId = userPublicId.Trim(),
                variables = new
                {
                    UserName = ResolveDisplayName(fullName, email),
                    VerificationUrl = verificationUrl
                }
            },
            SerializerOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: VerificationEmailRequestedEventType,
            aggregateType: AggregateTypeUserAccount,
            aggregateId: userId.ToString(),
            payload: payload,
            occurredAt: occurredAtUtc,
            priority: 5,
            aggregatePublicId: userPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: userPublicId.Trim(),
            initiatorUserId: userId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueuePasswordChangedEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateUserEnvelope(userId, userPublicId, email, occurredAtUtc);

        string payload = JsonSerializer.Serialize(
            new
            {
                businessDedupeKey = $"identity:password-changed:{userId}:{occurredAtUtc:O}",
                recipientUserId = userId,
                toEmail = email.Trim(),
                templateKey = "PasswordChanged",
                templateVersion = 1,
                subject = "Your password was changed",
                provider = "smtp",
                correlationId = userPublicId.Trim(),
                variables = new
                {
                    UserName = ResolveDisplayName(fullName, email),
                    ChangedAtUtc = occurredAtUtc.ToString("O")
                }
            },
            SerializerOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: PasswordChangedEmailRequestedEventType,
            aggregateType: AggregateTypeUserAccount,
            aggregateId: userId.ToString(),
            payload: payload,
            occurredAt: occurredAtUtc,
            priority: 5,
            aggregatePublicId: userPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: userPublicId.Trim(),
            initiatorUserId: userId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    public async Task EnqueuePasswordResetEmailAsync(
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string rawResetToken,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateUserEnvelope(userId, userPublicId, email, occurredAtUtc);

        if (string.IsNullOrWhiteSpace(rawResetToken))
        {
            throw new ArgumentException("Raw reset token is required.", nameof(rawResetToken));
        }

        string resetUrl = BuildResetUrl(rawResetToken);

        string payload = JsonSerializer.Serialize(
            new
            {
                businessDedupeKey = $"identity:password-reset:{userId}:{rawResetToken}",
                recipientUserId = userId,
                toEmail = email.Trim(),
                templateKey = "ResetPassword",
                templateVersion = 1,
                subject = "Reset your password",
                provider = "smtp",
                correlationId = userPublicId.Trim(),
                variables = new
                {
                    UserName = ResolveDisplayName(fullName, email),
                    ResetUrl = resetUrl
                }
            },
            SerializerOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: PasswordResetRequestedEventType,
            aggregateType: AggregateTypeUserAccount,
            aggregateId: userId.ToString(),
            payload: payload,
            occurredAt: occurredAtUtc,
            priority: 5,
            aggregatePublicId: userPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: userPublicId.Trim(),
            initiatorUserId: userId);

        await _outboxMessageRepository.InsertAsync(
            _unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static void ValidateUserEnvelope(
        long userId,
        string userPublicId,
        string email,
        DateTime occurredAtUtc)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(userPublicId))
        {
            throw new ArgumentException("User public id is required.", nameof(userPublicId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("OccurredAtUtc is required.", nameof(occurredAtUtc));
        }
    }

    private static string ResolveDisplayName(string? fullName, string email)
    {
        return string.IsNullOrWhiteSpace(fullName)
            ? email.Trim()
            : fullName.Trim();
    }

    private static string BuildVerificationUrl(string rawVerificationToken)
    {
        return $"{DevVerifyEmailEndpoint}?token={Uri.EscapeDataString(rawVerificationToken)}";
    }

    private static string BuildResetUrl(string rawResetToken)
    {
        return $"{DevResetPasswordEndpoint}?token={Uri.EscapeDataString(rawResetToken)}";
    }
}