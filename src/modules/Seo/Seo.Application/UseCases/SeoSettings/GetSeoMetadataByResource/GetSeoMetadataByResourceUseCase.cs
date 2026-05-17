using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Models.Results;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Constants;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataByResource;

public sealed class GetSeoMetadataByResourceUseCase : IGetSeoMetadataByResourceUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetSeoMetadataByResourceUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository
            ?? throw new ArgumentNullException(nameof(seoMetadataRepository));
    }

    public async Task<Result<GetSeoMetadataByResourceResponse>> ExecuteAsync(
        GetSeoMetadataByResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            string scope = string.IsNullOrWhiteSpace(request.Scope)
                ? SeoScopes.Public
                : request.Scope.Trim();

            if (!SeoScopes.IsValid(scope))
            {
                return Result<GetSeoMetadataByResourceResponse>.Failure(
                    SeoErrors.SeoMetadata.InvalidScope);
            }

            if (!SeoResourceTypes.IsValid(request.ResourceType))
            {
                return Result<GetSeoMetadataByResourceResponse>.Failure(
                    SeoErrors.Resource.InvalidResourceType);
            }

            if (string.IsNullOrWhiteSpace(request.ResourcePublicId) ||
                request.ResourcePublicId.Trim().Length != 26)
            {
                return Result<GetSeoMetadataByResourceResponse>.Failure(
                    SeoErrors.Resource.InvalidResourcePublicId);
            }

            SeoMetadataResult? result =
                await _seoMetadataRepository.SelectMetadataByResourceAsync(
                    scope: scope,
                    resourceType: request.ResourceType.Trim(),
                    resourcePublicId: request.ResourcePublicId.Trim(),
                    cancellationToken: cancellationToken);

            if (result is null)
            {
                return Result<GetSeoMetadataByResourceResponse>.Failure(
                    SeoErrors.SeoMetadata.NotFound);
            }

            return Result<GetSeoMetadataByResourceResponse>.Success(
                new GetSeoMetadataByResourceResponse
                {
                    Scope = result.Scope,
                    ResourceType = result.ResourceType,
                    ResourcePublicId = result.ResourcePublicId,
                    Slug = result.Slug,
                    CanonicalUrl = result.CanonicalUrl,
                    MetaTitle = result.MetaTitle,
                    MetaDescription = result.MetaDescription,
                    OgTitle = result.OgTitle,
                    OgDescription = result.OgDescription,
                    OgImageUrl = result.OgImageUrl,
                    TwitterTitle = result.TwitterTitle,
                    TwitterDescription = result.TwitterDescription,
                    TwitterImageUrl = result.TwitterImageUrl,
                    Robots = result.Robots,
                    IsManualOverride = result.IsManualOverride,
                    SourceAggregateVersion = result.SourceAggregateVersion,
                    LastAppliedMessageId = result.LastAppliedMessageId,
                    LastSyncedAtUtc = result.LastSyncedAtUtc,
                    Version = result.Version
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetSeoMetadataByResourceResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetSeoMetadataByResourceResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SeoMetadata.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.INVALID_SCOPE" => SeoErrors.SeoMetadata.InvalidScope,
            "SEO.INVALID_RESOURCE_TYPE" => SeoErrors.Resource.InvalidResourceType,
            "SEO.INVALID_RESOURCE_PUBLIC_ID" => SeoErrors.Resource.InvalidResourcePublicId,
            "SEO.STORE_UNAVAILABLE" => SeoErrors.Infrastructure.StoreUnavailable,
            _ => SeoErrors.ValidationFailed
        };
    }
}