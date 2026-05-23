using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;

namespace Seo.Application.Ports.Services;

public interface ISeoOutboxWriter
{
    Task<long> EnqueueSlugRouteChangedAsync(
        ISeoUnitOfWork unitOfWork,
        SlugRegistry route,
        long? actorUserId,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueSlugRouteDeactivatedAsync(
        ISeoUnitOfWork unitOfWork,
        SlugRegistry route,
        long? actorUserId,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<long> EnqueueMetadataUpdatedAsync(
        ISeoUnitOfWork unitOfWork,
        SeoMetadata metadata,
        long? actorUserId,
        string? correlationId,
        CancellationToken cancellationToken = default);
}
