using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Interaction.Payloads;
using Reading.Application.Models.Results;

namespace Reading.Application.Consumers.Interaction;

public interface IInteractionReadingEventIngestionService
{
    Task<Result<ArticleProjectionApplyResult>>
        IngestArticleCountersProjectionPublishedAsync(
            InteractionReadingEnvelopeContext context,
            ArticleCountersProjectionPublishedReadingPayload payload,
            CancellationToken cancellationToken = default);
}
