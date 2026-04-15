using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Entities;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataById;

public sealed class GetSeoMetadataByIdUseCase : IGetSeoMetadataByIdUseCase
{
    private readonly ISeoMetadataRepository _seoMetadataRepository;

    public GetSeoMetadataByIdUseCase(
        ISeoMetadataRepository seoMetadataRepository)
    {
        _seoMetadataRepository = seoMetadataRepository;
    }

    public async Task<Result<GetSeoMetadataByIdResponse>> ExecuteAsync(
        GetSeoMetadataByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.SeoId <= 0)
            {
                return Result<GetSeoMetadataByIdResponse>.Failure(
                    SeoErrors.SeoMetadata.InvalidSeoId);
            }

            SeoMetadata? existing = await _seoMetadataRepository.GetByIdAsync(
                request.SeoId,
                cancellationToken);

            if (existing is null)
            {
                return Result<GetSeoMetadataByIdResponse>.Failure(
                    SeoErrors.SeoMetadata.NotFound);
            }

            return Result<GetSeoMetadataByIdResponse>.Success(
                new GetSeoMetadataByIdResponse
                {
                    SeoId = existing.SeoId,
                    ArticleId = existing.ArticleId,
                    CanonicalUrl = existing.CanonicalUrl,
                    MetaTitle = existing.MetaTitle,
                    MetaDescription = existing.MetaDescription,
                    OgTitle = existing.OgTitle,
                    OgDescription = existing.OgDescription,
                    OgImageUrl = existing.OgImageUrl,
                    TwitterTitle = existing.TwitterTitle,
                    TwitterDescription = existing.TwitterDescription,
                    TwitterImageUrl = existing.TwitterImageUrl,
                    Version = existing.Version,
                    UpdatedAt = existing.UpdatedAt,
                    UpdatedByUserId = existing.UpdatedByUserId
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetSeoMetadataByIdResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<GetSeoMetadataByIdResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SEO_METADATA_INVALID_SEO_ID" => SeoErrors.SeoMetadata.InvalidSeoId,
            "SEO.SEO_METADATA_INVALID_ARTICLE_ID" => SeoErrors.SeoMetadata.InvalidArticleId,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => SeoErrors.ValidationFailed
        };
    }
}