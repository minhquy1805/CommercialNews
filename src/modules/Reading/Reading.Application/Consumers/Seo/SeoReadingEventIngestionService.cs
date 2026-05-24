using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Consumers.Seo.Payloads;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.UseCases.Projections.ApplyArticleSeoMetadataProjection;
using Reading.Application.UseCases.Projections.ApplyArticleSeoRouteProjection;

namespace Reading.Application.Consumers.Seo;

public sealed class SeoReadingEventIngestionService
    : ISeoReadingEventIngestionService
{
    private readonly IApplyArticleSeoRouteProjectionUseCase
        _applyArticleSeoRouteProjectionUseCase;

    private readonly IApplyArticleSeoMetadataProjectionUseCase
        _applyArticleSeoMetadataProjectionUseCase;

    public SeoReadingEventIngestionService(
        IApplyArticleSeoRouteProjectionUseCase applyArticleSeoRouteProjectionUseCase,
        IApplyArticleSeoMetadataProjectionUseCase applyArticleSeoMetadataProjectionUseCase)
    {
        _applyArticleSeoRouteProjectionUseCase =
            applyArticleSeoRouteProjectionUseCase
            ?? throw new ArgumentNullException(
                nameof(applyArticleSeoRouteProjectionUseCase));

        _applyArticleSeoMetadataProjectionUseCase =
            applyArticleSeoMetadataProjectionUseCase
            ?? throw new ArgumentNullException(
                nameof(applyArticleSeoMetadataProjectionUseCase));
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestSlugRouteChangedAsync(
        SeoReadingEnvelopeContext context,
        SlugRouteChangedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new ApplyArticleSeoRouteProjectionCommand(
            Scope: payload.Scope,
            ResourceType: payload.ResourceType,
            ResourcePublicId: payload.ResourcePublicId,
            Slug: payload.Slug,
            CanonicalUrl: payload.CanonicalUrl,
            IsActive: payload.IsActive,
            IsIndexable: payload.IsIndexable,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleSeoRouteProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestSlugRouteDeactivatedAsync(
        SeoReadingEnvelopeContext context,
        SlugRouteDeactivatedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        /*
          A deactivated route is always projected into the safe terminal state.
          Reading must never expose or index a deactivated SEO route.
        */
        var command = new ApplyArticleSeoRouteProjectionCommand(
            Scope: payload.Scope,
            ResourceType: payload.ResourceType,
            ResourcePublicId: payload.ResourcePublicId,
            Slug: payload.Slug,
            CanonicalUrl: payload.CanonicalUrl,
            IsActive: false,
            IsIndexable: false,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleSeoRouteProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }

    public Task<Result<ArticleProjectionApplyResult>> IngestMetadataUpdatedAsync(
        SeoReadingEnvelopeContext context,
        SeoMetadataUpdatedReadingPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var command = new ApplyArticleSeoMetadataProjectionCommand(
            Scope: payload.Scope,
            ResourceType: payload.ResourceType,
            ResourcePublicId: payload.ResourcePublicId,
            MetaTitle: payload.MetaTitle,
            MetaDescription: payload.MetaDescription,
            OgTitle: payload.OgTitle,
            OgDescription: payload.OgDescription,
            OgImageUrl: payload.OgImageUrl,
            TwitterTitle: payload.TwitterTitle,
            TwitterDescription: payload.TwitterDescription,
            TwitterImageUrl: payload.TwitterImageUrl,
            Robots: payload.Robots,
            IsManualOverride: payload.IsManualOverride,
            SourceVersion: payload.Version,
            MessageId: context.MessageId,
            SourceOccurredAtUtc: context.OccurredAtUtc);

        return _applyArticleSeoMetadataProjectionUseCase.ExecuteAsync(
            command,
            cancellationToken);
    }
}