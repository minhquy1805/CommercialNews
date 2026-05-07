using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using Identity.Application.Outbox;
using Identity.Application.Outbox.Payloads;
using Identity.Application.Ports.Persistence;
using Identity.Application.Ports.Services;

namespace Identity.Infrastructure.Services;

public sealed class IdentityOutboxWriter : IIdentityOutboxWriter
{
    private const string AggregateTypeUserAccount = "Identity.UserAccount";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public IdentityOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public async Task<long> EnqueueVerificationEmailRequestedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        long verificationTokenId,
        string rawVerificationToken,
        DateTime expiresAtUtc,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateUserEnvelope(userId, userPublicId, email, occurredAtUtc);
        ValidatePositiveId(verificationTokenId, nameof(verificationTokenId));
        ValidateRequired(rawVerificationToken, nameof(rawVerificationToken));
        ValidateRequiredDate(expiresAtUtc, nameof(expiresAtUtc));

        string businessDedupeKey =
            $"identity:verification-email:{userId}:{verificationTokenId}";

        var payload = new VerificationEmailRequestedIntegrationEventPayload(
            UserId: userId,
            UserPublicId: userPublicId.Trim(),
            Email: email.Trim(),
            FullName: NormalizeOptional(fullName),
            VerificationTokenId: verificationTokenId,
            RawVerificationToken: rawVerificationToken.Trim(),
            ExpiresAtUtc: expiresAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.VerificationEmailRequested,
            userId: userId,
            userPublicId: userPublicId,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 1,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueuePasswordResetRequestedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        long resetTokenId,
        string rawResetToken,
        DateTime expiresAtUtc,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateUserEnvelope(userId, userPublicId, email, occurredAtUtc);
        ValidatePositiveId(resetTokenId, nameof(resetTokenId));
        ValidateRequired(rawResetToken, nameof(rawResetToken));
        ValidateRequiredDate(expiresAtUtc, nameof(expiresAtUtc));

        string businessDedupeKey =
            $"identity:password-reset-email:{userId}:{resetTokenId}";

        var payload = new PasswordResetRequestedIntegrationEventPayload(
            UserId: userId,
            UserPublicId: userPublicId.Trim(),
            Email: email.Trim(),
            FullName: NormalizeOptional(fullName),
            ResetTokenId: resetTokenId,
            RawResetToken: rawResetToken.Trim(),
            ExpiresAtUtc: expiresAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.PasswordResetRequested,
            userId: userId,
            userPublicId: userPublicId,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 1,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueuePasswordChangedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        string reason,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateUserEnvelope(userId, userPublicId, email, occurredAtUtc);
        ValidateRequired(reason, nameof(reason));

        string normalizedReason = reason.Trim();

        string businessDedupeKey =
            $"identity:password-changed:{userId}:{occurredAtUtc.Ticks}:{normalizedReason}";

        var payload = new PasswordChangedIntegrationEventPayload(
            UserId: userId,
            UserPublicId: userPublicId.Trim(),
            Email: email.Trim(),
            FullName: NormalizeOptional(fullName),
            Reason: normalizedReason,
            ChangedAtUtc: occurredAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.PasswordChanged,
            userId: userId,
            userPublicId: userPublicId,
            payload: payload,
            occurredAtUtc: occurredAtUtc,
            priority: 3,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueueEmailVerifiedAsync(
        IIdentityUnitOfWork unitOfWork,
        long userId,
        string userPublicId,
        string email,
        string? fullName,
        long verificationTokenId,
        DateTime verifiedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateUserEnvelope(userId, userPublicId, email, verifiedAtUtc);
        ValidatePositiveId(verificationTokenId, nameof(verificationTokenId));

        string businessDedupeKey =
            $"identity:email-verified:{userId}:{verificationTokenId}";

        var payload = new EmailVerifiedIntegrationEventPayload(
            UserId: userId,
            UserPublicId: userPublicId.Trim(),
            Email: email.Trim(),
            FullName: NormalizeOptional(fullName),
            VerificationTokenId: verificationTokenId,
            VerifiedAtUtc: verifiedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.EmailVerified,
            userId: userId,
            userPublicId: userPublicId,
            payload: payload,
            occurredAtUtc: verifiedAtUtc,
            priority: 5,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueueUserActivatedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        DateTime activatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateAdminTargetEnvelope(
            targetUserId,
            targetUserPublicId,
            targetEmail,
            actorUserId,
            activatedAtUtc);
        ValidateRequired(previousStatus, nameof(previousStatus));
        ValidateRequired(newStatus, nameof(newStatus));

        string businessDedupeKey =
            $"identity:admin:user-activated:{targetUserId}:{actorUserId}:{activatedAtUtc.Ticks}";

        var payload = new UserActivatedIntegrationEventPayload(
            TargetUserId: targetUserId,
            TargetUserPublicId: targetUserPublicId.Trim(),
            TargetEmail: targetEmail.Trim(),
            TargetFullName: NormalizeOptional(targetFullName),
            ActorUserId: actorUserId,
            Reason: NormalizeOptional(reason),
            PreviousStatus: previousStatus.Trim(),
            NewStatus: newStatus.Trim(),
            ActivatedAtUtc: activatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.UserActivated,
            userId: targetUserId,
            userPublicId: targetUserPublicId,
            payload: payload,
            occurredAtUtc: activatedAtUtc,
            priority: 1,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueueUserDisabledAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        bool sessionsRevoked,
        int revokedSessionCount,
        DateTime disabledAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateAdminTargetEnvelope(
            targetUserId,
            targetUserPublicId,
            targetEmail,
            actorUserId,
            disabledAtUtc);
        ValidateRequired(previousStatus, nameof(previousStatus));
        ValidateRequired(newStatus, nameof(newStatus));
        ValidateNonNegative(revokedSessionCount, nameof(revokedSessionCount));

        string businessDedupeKey =
            $"identity:admin:user-disabled:{targetUserId}:{actorUserId}:{disabledAtUtc.Ticks}";

        var payload = new UserDisabledIntegrationEventPayload(
            TargetUserId: targetUserId,
            TargetUserPublicId: targetUserPublicId.Trim(),
            TargetEmail: targetEmail.Trim(),
            TargetFullName: NormalizeOptional(targetFullName),
            ActorUserId: actorUserId,
            Reason: NormalizeOptional(reason),
            PreviousStatus: previousStatus.Trim(),
            NewStatus: newStatus.Trim(),
            SessionsRevoked: sessionsRevoked,
            RevokedSessionCount: revokedSessionCount,
            DisabledAtUtc: disabledAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.UserDisabled,
            userId: targetUserId,
            userPublicId: targetUserPublicId,
            payload: payload,
            occurredAtUtc: disabledAtUtc,
            priority: 1,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueueUserLockedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        DateTime lockedUntilUtc,
        bool sessionsRevoked,
        int revokedSessionCount,
        DateTime lockedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateAdminTargetEnvelope(
            targetUserId,
            targetUserPublicId,
            targetEmail,
            actorUserId,
            lockedAtUtc);
        ValidateRequired(previousStatus, nameof(previousStatus));
        ValidateRequired(newStatus, nameof(newStatus));
        ValidateRequiredDate(lockedUntilUtc, nameof(lockedUntilUtc));
        ValidateNonNegative(revokedSessionCount, nameof(revokedSessionCount));

        string businessDedupeKey =
            $"identity:admin:user-locked:{targetUserId}:{actorUserId}:{lockedAtUtc.Ticks}";

        var payload = new UserLockedIntegrationEventPayload(
            TargetUserId: targetUserId,
            TargetUserPublicId: targetUserPublicId.Trim(),
            TargetEmail: targetEmail.Trim(),
            TargetFullName: NormalizeOptional(targetFullName),
            ActorUserId: actorUserId,
            Reason: NormalizeOptional(reason),
            PreviousStatus: previousStatus.Trim(),
            NewStatus: newStatus.Trim(),
            LockedUntilUtc: lockedUntilUtc,
            SessionsRevoked: sessionsRevoked,
            RevokedSessionCount: revokedSessionCount,
            LockedAtUtc: lockedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.UserLocked,
            userId: targetUserId,
            userPublicId: targetUserPublicId,
            payload: payload,
            occurredAtUtc: lockedAtUtc,
            priority: 1,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueueUserUnlockedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        string previousStatus,
        string newStatus,
        DateTime unlockedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateAdminTargetEnvelope(
            targetUserId,
            targetUserPublicId,
            targetEmail,
            actorUserId,
            unlockedAtUtc);
        ValidateRequired(previousStatus, nameof(previousStatus));
        ValidateRequired(newStatus, nameof(newStatus));

        string businessDedupeKey =
            $"identity:admin:user-unlocked:{targetUserId}:{actorUserId}:{unlockedAtUtc.Ticks}";

        var payload = new UserUnlockedIntegrationEventPayload(
            TargetUserId: targetUserId,
            TargetUserPublicId: targetUserPublicId.Trim(),
            TargetEmail: targetEmail.Trim(),
            TargetFullName: NormalizeOptional(targetFullName),
            ActorUserId: actorUserId,
            Reason: NormalizeOptional(reason),
            PreviousStatus: previousStatus.Trim(),
            NewStatus: newStatus.Trim(),
            UnlockedAtUtc: unlockedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.UserUnlocked,
            userId: targetUserId,
            userPublicId: targetUserPublicId,
            payload: payload,
            occurredAtUtc: unlockedAtUtc,
            priority: 1,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueueEmailMarkedVerifiedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        bool wasAlreadyVerified,
        string previousStatus,
        string newStatus,
        DateTime markedVerifiedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateAdminTargetEnvelope(
            targetUserId,
            targetUserPublicId,
            targetEmail,
            actorUserId,
            markedVerifiedAtUtc);
        ValidateRequired(previousStatus, nameof(previousStatus));
        ValidateRequired(newStatus, nameof(newStatus));

        string businessDedupeKey =
            $"identity:admin:email-marked-verified:{targetUserId}:{actorUserId}:{markedVerifiedAtUtc.Ticks}";

        var payload = new EmailMarkedVerifiedIntegrationEventPayload(
            TargetUserId: targetUserId,
            TargetUserPublicId: targetUserPublicId.Trim(),
            TargetEmail: targetEmail.Trim(),
            TargetFullName: NormalizeOptional(targetFullName),
            ActorUserId: actorUserId,
            Reason: NormalizeOptional(reason),
            WasAlreadyVerified: wasAlreadyVerified,
            PreviousStatus: previousStatus.Trim(),
            NewStatus: newStatus.Trim(),
            MarkedVerifiedAtUtc: markedVerifiedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.EmailMarkedVerified,
            userId: targetUserId,
            userPublicId: targetUserPublicId,
            payload: payload,
            occurredAtUtc: markedVerifiedAtUtc,
            priority: 1,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public async Task<long> EnqueueUserSessionsRevokedAsync(
        IIdentityUnitOfWork unitOfWork,
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        string? targetFullName,
        long actorUserId,
        string? reason,
        int revokedSessionCount,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        ValidateAdminTargetEnvelope(
            targetUserId,
            targetUserPublicId,
            targetEmail,
            actorUserId,
            revokedAtUtc);
        ValidateNonNegative(revokedSessionCount, nameof(revokedSessionCount));

        string businessDedupeKey =
            $"identity:admin:user-sessions-revoked:{targetUserId}:{actorUserId}:{revokedAtUtc.Ticks}";

        var payload = new UserSessionsRevokedIntegrationEventPayload(
            TargetUserId: targetUserId,
            TargetUserPublicId: targetUserPublicId.Trim(),
            TargetEmail: targetEmail.Trim(),
            TargetFullName: NormalizeOptional(targetFullName),
            ActorUserId: actorUserId,
            Reason: NormalizeOptional(reason),
            RevokedSessionCount: revokedSessionCount,
            RevokedAtUtc: revokedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return await InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: IdentityIntegrationEventTypes.UserSessionsRevoked,
            userId: targetUserId,
            userPublicId: targetUserPublicId,
            payload: payload,
            occurredAtUtc: revokedAtUtc,
            priority: 1,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    private async Task<long> InsertOutboxMessageAsync<TPayload>(
        IIdentityUnitOfWork unitOfWork,
        string eventType,
        long userId,
        string userPublicId,
        TPayload payload,
        DateTime occurredAtUtc,
        byte priority,
        CancellationToken cancellationToken,
        long? initiatorUserId = null,
        string? correlationId = null)
    {
        if (!unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Identity outbox message must be written inside an active transaction.");
        }

        string payloadJson = JsonSerializer.Serialize(
            payload,
            SerializerOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: eventType,
            aggregateType: AggregateTypeUserAccount,
            aggregateId: userId.ToString(),
            payload: payloadJson,
            occurredAt: occurredAtUtc,
            priority: priority,
            aggregatePublicId: userPublicId.Trim(),
            aggregateVersion: null,
            headers: null,
            correlationId: NormalizeOptional(correlationId),
            initiatorUserId: initiatorUserId ?? userId);

        return await _outboxMessageRepository.InsertAsync(
            unitOfWork,
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

        ValidateRequired(userPublicId, nameof(userPublicId));
        ValidateRequired(email, nameof(email));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidateAdminTargetEnvelope(
        long targetUserId,
        string targetUserPublicId,
        string targetEmail,
        long actorUserId,
        DateTime occurredAtUtc)
    {
        ValidateUserEnvelope(
            targetUserId,
            targetUserPublicId,
            targetEmail,
            occurredAtUtc);
        ValidatePositiveId(actorUserId, nameof(actorUserId));
    }

    private static void ValidatePositiveId(
        long value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateNonNegative(
        int value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateRequired(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }
    }

    private static void ValidateRequiredDate(
        DateTime value,
        string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
