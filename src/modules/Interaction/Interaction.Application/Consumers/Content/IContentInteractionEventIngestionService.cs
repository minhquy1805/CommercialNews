using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Interaction.Application.Consumers.Content.Payloads;
using Interaction.Application.Contracts.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;

namespace Interaction.Application.Consumers.Content;

public interface IContentInteractionEventIngestionService
{
    Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticlePublishedAsync(
            ContentInteractionEnvelopeContext context,
            ArticlePublishedInteractionPayload payload,
            CancellationToken cancellationToken = default);

    Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticleUnpublishedAsync(
            ContentInteractionEnvelopeContext context,
            ArticleUnpublishedInteractionPayload payload,
            CancellationToken cancellationToken = default);

    Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticleArchivedAsync(
            ContentInteractionEnvelopeContext context,
            ArticleArchivedInteractionPayload payload,
            CancellationToken cancellationToken = default);

    Task<Result<ApplyArticleInteractionTargetProjectionResponseDto>>
        IngestArticleSoftDeletedAsync(
            ContentInteractionEnvelopeContext context,
            ArticleSoftDeletedInteractionPayload payload,
            CancellationToken cancellationToken = default);
}