using System.Text.Json;
using CommercialNews.BuildingBlocks.Outbox.Models;
using CommercialNews.BuildingBlocks.Outbox.Ports;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using Seo.Application.Outbox;
using Seo.Application.Outbox.Payloads;
using Seo.Application.Ports.Persistence;
using Seo.Application.Ports.Services;
using Seo.Domain.Entities;

namespace Seo.Infrastructure.Services;

public sealed class SeoOutboxWriter : ISeoOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IPublicIdGenerator _publicIdGenerator;

    public SeoOutboxWriter(
        IOutboxMessageRepository outboxMessageRepository,
        IPublicIdGenerator publicIdGenerator)
    {
        _outboxMessageRepository = outboxMessageRepository
            ?? throw new ArgumentNullException(nameof(outboxMessageRepository));

        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
    }

    public Task<long> EnqueueSlugRouteChangedAsync(
        ISeoUnitOfWork unitOfWork,
        SlugRegistry route,
        long? actorUserId,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(route);

        DateTime changedAtUtc = route.UpdatedAtUtc;

        ValidateRoute(route, changedAtUtc);

        string businessDedupeKey = BuildSlugRouteBusinessDedupeKey(
            route,
            "changed");

        var payload = new SlugRouteChangedIntegrationEventPayload(
            Scope: route.Scope,
            ResourceType: route.ResourceType,
            ResourcePublicId: route.ResourcePublicId,
            Slug: route.Slug,
            CanonicalUrl: route.CanonicalUrl,
            IsActive: route.IsActive,
            IsIndexable: route.IsIndexable,
            ActorUserId: actorUserId,
            Version: route.Version,
            ChangedAtUtc: changedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: SeoIntegrationEventTypes.SlugRouteChanged,
            aggregateType: SeoAggregateTypes.SlugRegistry,
            aggregateId: route.ResourcePublicId,
            aggregatePublicId: route.ResourcePublicId,
            aggregateVersion: route.Version,
            payload: payload,
            occurredAtUtc: changedAtUtc,
            priority: 2,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueSlugRouteDeactivatedAsync(
        ISeoUnitOfWork unitOfWork,
        SlugRegistry route,
        long? actorUserId,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(route);

        DateTime deactivatedAtUtc = route.UpdatedAtUtc;

        ValidateRoute(route, deactivatedAtUtc);
        ValidateDeactivatedRoute(route);

        string businessDedupeKey = BuildSlugRouteBusinessDedupeKey(
            route,
            "deactivated");

        var payload = new SlugRouteDeactivatedIntegrationEventPayload(
            Scope: route.Scope,
            ResourceType: route.ResourceType,
            ResourcePublicId: route.ResourcePublicId,
            Slug: route.Slug,
            CanonicalUrl: route.CanonicalUrl,
            IsActive: route.IsActive,
            IsIndexable: route.IsIndexable,
            ActorUserId: actorUserId,
            Version: route.Version,
            DeactivatedAtUtc: deactivatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: SeoIntegrationEventTypes.SlugRouteDeactivated,
            aggregateType: SeoAggregateTypes.SlugRegistry,
            aggregateId: route.ResourcePublicId,
            aggregatePublicId: route.ResourcePublicId,
            aggregateVersion: route.Version,
            payload: payload,
            occurredAtUtc: deactivatedAtUtc,
            priority: 1,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    public Task<long> EnqueueMetadataUpdatedAsync(
        ISeoUnitOfWork unitOfWork,
        SeoMetadata metadata,
        long? actorUserId,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(metadata);

        DateTime updatedAtUtc = metadata.UpdatedAtUtc;

        ValidateMetadata(metadata, updatedAtUtc);

        string businessDedupeKey = BuildMetadataBusinessDedupeKey(metadata);

        var payload = new SeoMetadataUpdatedIntegrationEventPayload(
            Scope: metadata.Scope,
            ResourceType: metadata.ResourceType,
            ResourcePublicId: metadata.ResourcePublicId,
            MetaTitle: metadata.MetaTitle,
            MetaDescription: metadata.MetaDescription,
            OgTitle: metadata.OgTitle,
            OgDescription: metadata.OgDescription,
            OgImageUrl: metadata.OgImageUrl,
            TwitterTitle: metadata.TwitterTitle,
            TwitterDescription: metadata.TwitterDescription,
            TwitterImageUrl: metadata.TwitterImageUrl,
            Robots: metadata.Robots,
            IsManualOverride: metadata.IsManualOverride,
            ActorUserId: actorUserId,
            Version: metadata.Version,
            UpdatedAtUtc: updatedAtUtc,
            BusinessDedupeKey: businessDedupeKey);

        return InsertOutboxMessageAsync(
            unitOfWork: unitOfWork,
            eventType: SeoIntegrationEventTypes.MetadataUpdated,
            aggregateType: SeoAggregateTypes.SeoMetadata,
            aggregateId: metadata.ResourcePublicId,
            aggregatePublicId: metadata.ResourcePublicId,
            aggregateVersion: metadata.Version,
            payload: payload,
            occurredAtUtc: updatedAtUtc,
            priority: 3,
            correlationId: correlationId,
            initiatorUserId: actorUserId,
            cancellationToken: cancellationToken);
    }

    private async Task<long> InsertOutboxMessageAsync<TPayload>(
        ISeoUnitOfWork unitOfWork,
        string eventType,
        string aggregateType,
        string aggregateId,
        string? aggregatePublicId,
        long aggregateVersion,
        TPayload payload,
        DateTime occurredAtUtc,
        byte priority,
        string? correlationId,
        long? initiatorUserId,
        CancellationToken cancellationToken)
    {
        if (!unitOfWork.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "SEO outbox message must be written inside an active transaction.");
        }

        ValidateRequired(eventType, nameof(eventType));
        ValidateRequired(aggregateType, nameof(aggregateType));
        ValidateRequired(aggregateId, nameof(aggregateId));
        ValidatePositiveVersion(aggregateVersion, nameof(aggregateVersion));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));

        string payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            messageId: _publicIdGenerator.NewId(),
            eventType: eventType.Trim(),
            aggregateType: aggregateType.Trim(),
            aggregateId: aggregateId.Trim(),
            payload: payloadJson,
            occurredAt: occurredAtUtc,
            priority: priority,
            aggregatePublicId: NormalizeOptional(aggregatePublicId),
            aggregateVersion: ToAggregateVersion(aggregateVersion),
            headers: null,
            correlationId: NormalizeOptional(correlationId),
            initiatorUserId: initiatorUserId);

        return await _outboxMessageRepository.InsertAsync(
            unitOfWork,
            outboxMessage,
            cancellationToken);
    }

    private static void ValidateRoute(
        SlugRegistry route,
        DateTime occurredAtUtc)
    {
        ValidateRequired(route.Scope, nameof(route.Scope));
        ValidateRequired(route.ResourceType, nameof(route.ResourceType));
        ValidatePublicId(route.ResourcePublicId, nameof(route.ResourcePublicId));
        ValidateRequired(route.Slug, nameof(route.Slug));
        ValidatePositiveVersion(route.Version, nameof(route.Version));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static void ValidateDeactivatedRoute(SlugRegistry route)
    {
        if (route.IsActive)
        {
            throw new InvalidOperationException(
                "A deactivated slug route event cannot be emitted for an active route.");
        }

        if (route.IsIndexable)
        {
            throw new InvalidOperationException(
                "A deactivated slug route event cannot be emitted for an indexable route.");
        }
    }

    private static void ValidateMetadata(
        SeoMetadata metadata,
        DateTime occurredAtUtc)
    {
        ValidateRequired(metadata.Scope, nameof(metadata.Scope));
        ValidateRequired(metadata.ResourceType, nameof(metadata.ResourceType));
        ValidatePublicId(metadata.ResourcePublicId, nameof(metadata.ResourcePublicId));
        ValidatePositiveVersion(metadata.Version, nameof(metadata.Version));
        ValidateRequiredDate(occurredAtUtc, nameof(occurredAtUtc));
    }

    private static string BuildSlugRouteBusinessDedupeKey(
        SlugRegistry route,
        string action)
    {
        return "seo:slug-route:" +
               $"{route.Scope.Trim()}:{route.ResourceType.Trim()}:" +
               $"{route.ResourcePublicId.Trim()}:{action}:v{route.Version}";
    }

    private static string BuildMetadataBusinessDedupeKey(SeoMetadata metadata)
    {
        return "seo:metadata:" +
               $"{metadata.Scope.Trim()}:{metadata.ResourceType.Trim()}:" +
               $"{metadata.ResourcePublicId.Trim()}:updated:v{metadata.Version}";
    }

    private static int ToAggregateVersion(long version)
    {
        if (version > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version),
                "Aggregate version exceeds Int32 range.");
        }

        return (int)version;
    }

    private static void ValidatePublicId(string? value, string parameterName)
    {
        ValidateRequired(value, parameterName);

        if (value!.Trim().Length != 26)
        {
            throw new ArgumentException(
                $"{parameterName} must be exactly 26 characters.",
                parameterName);
        }
    }

    private static void ValidateRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }
    }

    private static void ValidatePositiveVersion(
        long value,
        string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateRequiredDate(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
