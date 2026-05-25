using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Identity.Payloads;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.UseCases.Projections.ApplyAuthorProfileProjection;

namespace Reading.Application.Consumers.Identity;

public sealed class IdentityReadingEventIngestionService
    : IIdentityReadingEventIngestionService
{
    private const string IdentityUserAccountAggregateType =
        "Identity.UserAccount";

    private readonly IApplyAuthorProfileProjectionUseCase
        _applyAuthorProfileProjectionUseCase;

    public IdentityReadingEventIngestionService(
        IApplyAuthorProfileProjectionUseCase applyAuthorProfileProjectionUseCase)
    {
        _applyAuthorProfileProjectionUseCase =
            applyAuthorProfileProjectionUseCase
            ?? throw new ArgumentNullException(
                nameof(applyAuthorProfileProjectionUseCase));
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestUserRegisteredAsync(
        IdentityReadingEnvelopeContext context,
        UserRegisteredReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        Error? envelopeError = ValidateEnvelopeIdentity(
            context,
            payload.UserId,
            payload.UserPublicId,
            payload.Version);

        if (envelopeError is not null)
        {
            return Task.FromResult(
                Result<ArticleProjectionApplyResult>.Failure(envelopeError));
        }

        var command = new ApplyAuthorProfileProjectionCommand(
            AuthorUserId: payload.UserId,
            AuthorUserPublicId: payload.UserPublicId,
            AuthorDisplayName: payload.FullName,
            AuthorAvatarUrl: null,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyAuthorProfileProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestUserPublicProfileUpdatedAsync(
        IdentityReadingEnvelopeContext context,
        UserPublicProfileUpdatedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        Error? envelopeError = ValidateEnvelopeIdentity(
            context,
            payload.UserId,
            payload.UserPublicId,
            payload.Version);

        if (envelopeError is not null)
        {
            return Task.FromResult(
                Result<ArticleProjectionApplyResult>.Failure(envelopeError));
        }

        var command = new ApplyAuthorProfileProjectionCommand(
            AuthorUserId: payload.UserId,
            AuthorUserPublicId: payload.UserPublicId,
            AuthorDisplayName: payload.FullName,
            AuthorAvatarUrl: payload.AvatarUrl,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyAuthorProfileProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    private static Error? ValidateEnvelopeIdentity(
        IdentityReadingEnvelopeContext context,
        long payloadUserId,
        string payloadUserPublicId,
        int payloadVersion)
    {
        if (!string.Equals(
                context.AggregateType,
                IdentityUserAccountAggregateType,
                StringComparison.OrdinalIgnoreCase))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!long.TryParse(context.AggregateId, out long aggregateUserId)
            || aggregateUserId != payloadUserId)
        {
            return ReadingErrors.ValidationFailed;
        }

        if (string.IsNullOrWhiteSpace(context.AggregatePublicId)
            || !string.Equals(
                context.AggregatePublicId.Trim(),
                payloadUserPublicId?.Trim(),
                StringComparison.Ordinal))
        {
            return ReadingErrors.ValidationFailed;
        }

        if (!context.AggregateVersion.HasValue
            || context.AggregateVersion.Value <= 0
            || context.AggregateVersion.Value != payloadVersion)
        {
            return ReadingErrors.Projection.InvalidSourceVersion;
        }

        return null;
    }
}