using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Consumers.Content.Payloads;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;
using Interaction.Application.UseCases.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace Interaction.Application.Consumers.Content;

public sealed class ContentInteractionEventIngestionService
    : IContentInteractionEventIngestionService
{
    private readonly IApplyArticleInteractionTargetProjectionUseCase
        _applyArticleInteractionTargetProjectionUseCase;

    public ContentInteractionEventIngestionService(
        IApplyArticleInteractionTargetProjectionUseCase
            applyArticleInteractionTargetProjectionUseCase)
    {
        _applyArticleInteractionTargetProjectionUseCase =
            applyArticleInteractionTargetProjectionUseCase
            ?? throw new ArgumentNullException(
                nameof(applyArticleInteractionTargetProjectionUseCase));
    }

    public Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticlePublishedAsync(
            ContentInteractionEnvelopeContext context,
            ArticlePublishedInteractionPayload payload,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var request = new ApplyArticleInteractionTargetProjectionRequestDto
        {
            ArticlePublicId = payload.ArticlePublicId,
            SourceStatus = payload.ToStatus,
            IsInteractionEnabled = true,
            SourceVersion = payload.Version,
            SourceMessageId = context.MessageId,
            SourceOccurredAtUtc = context.OccurredAtUtc
        };

        return _applyArticleInteractionTargetProjectionUseCase.ExecuteAsync(
            request,
            cancellationToken);
    }

    public Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticleUnpublishedAsync(
            ContentInteractionEnvelopeContext context,
            ArticleUnpublishedInteractionPayload payload,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var request = new ApplyArticleInteractionTargetProjectionRequestDto
        {
            ArticlePublicId = payload.ArticlePublicId,
            SourceStatus = payload.ToStatus,
            IsInteractionEnabled = false,
            SourceVersion = payload.Version,
            SourceMessageId = context.MessageId,
            SourceOccurredAtUtc = context.OccurredAtUtc
        };

        return _applyArticleInteractionTargetProjectionUseCase.ExecuteAsync(
            request,
            cancellationToken);
    }

    public Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticleArchivedAsync(
            ContentInteractionEnvelopeContext context,
            ArticleArchivedInteractionPayload payload,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var request = new ApplyArticleInteractionTargetProjectionRequestDto
        {
            ArticlePublicId = payload.ArticlePublicId,
            SourceStatus = payload.ToStatus,
            IsInteractionEnabled = false,
            SourceVersion = payload.Version,
            SourceMessageId = context.MessageId,
            SourceOccurredAtUtc = context.OccurredAtUtc
        };

        return _applyArticleInteractionTargetProjectionUseCase.ExecuteAsync(
            request,
            cancellationToken);
    }

    public Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticleSoftDeletedAsync(
            ContentInteractionEnvelopeContext context,
            ArticleSoftDeletedInteractionPayload payload,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var request = new ApplyArticleInteractionTargetProjectionRequestDto
        {
            ArticlePublicId = payload.ArticlePublicId,
            SourceStatus = payload.ToStatus,
            IsInteractionEnabled = false,
            SourceVersion = payload.Version,
            SourceMessageId = context.MessageId,
            SourceOccurredAtUtc = context.OccurredAtUtc
        };

        return _applyArticleInteractionTargetProjectionUseCase.ExecuteAsync(
            request,
            cancellationToken);
    }
}