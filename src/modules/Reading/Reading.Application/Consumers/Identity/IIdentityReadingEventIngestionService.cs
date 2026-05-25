using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Identity.Payloads;
using Reading.Application.Models.Results;

namespace Reading.Application.Consumers.Identity;

public interface IIdentityReadingEventIngestionService
{
    Task<Result<ArticleProjectionApplyResult>> IngestUserRegisteredAsync(
        IdentityReadingEnvelopeContext context,
        UserRegisteredReadingPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<ArticleProjectionApplyResult>> IngestUserPublicProfileUpdatedAsync(
        IdentityReadingEnvelopeContext context,
        UserPublicProfileUpdatedReadingPayload payload,
        CancellationToken cancellationToken = default);
}
