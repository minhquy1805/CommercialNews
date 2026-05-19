using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.GetMediaUsage;

public sealed class GetMediaUsageUseCase : IGetMediaUsageUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;

    public GetMediaUsageUseCase(
        IArticleMediaRepository articleMediaRepository)
    {
        _articleMediaRepository = articleMediaRepository
            ?? throw new ArgumentNullException(nameof(articleMediaRepository));
    }

    public async Task<Result<GetMediaUsageResponse>> ExecuteAsync(
        GetMediaUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.MediaId <= 0)
            {
                return Result<GetMediaUsageResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidMediaId);
            }

            IReadOnlyList<ArticleMediaUsageResultItem> usages =
                await _articleMediaRepository.SelectByMediaIdAsync(
                    mediaId: request.MediaId,
                    includeDeleted: request.IncludeDeleted,
                    cancellationToken: cancellationToken);

            return Result<GetMediaUsageResponse>.Success(
                new GetMediaUsageResponse
                {
                    MediaId = request.MediaId,
                    Items = usages.Select(MapItem).ToArray()
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetMediaUsageResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetMediaUsageResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static GetMediaUsageItemResponse MapItem(
        ArticleMediaUsageResultItem item)
    {
        return new GetMediaUsageItemResponse
        {
            ArticleMediaId = item.ArticleMediaId,

            ArticleId = item.ArticleId,
            AttachmentSetVersion = item.AttachmentSetVersion,

            MediaId = item.MediaId,

            SortOrder = item.SortOrder,
            IsPrimary = item.IsPrimary,

            AltTextOverride = item.AltTextOverride,
            Caption = item.Caption,

            CreatedAt = item.CreatedAt,
            CreatedBy = item.CreatedBy,

            UpdatedAt = item.UpdatedAt,
            UpdatedBy = item.UpdatedBy,

            Version = item.Version,

            IsDeleted = item.IsDeleted,
            DeletedAt = item.DeletedAt,
            DeletedBy = item.DeletedBy
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_MEDIA_ID" =>
                MediaErrors.ArticleMedia.InvalidMediaId,

            "MEDIA.MEDIA_VARIANT_INVALID_MEDIA_ID" =>
                MediaErrors.Variant.InvalidMediaId,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.MEDIA_NOT_FOUND" =>
                MediaErrors.MediaAsset.NotFound,

            "MEDIA.CONSTRAINT_VIOLATION" =>
                MediaErrors.ConstraintViolation,

            "MEDIA.CONCURRENT_MODIFICATION" =>
                MediaErrors.ConcurrentModification,

            "MEDIA.DEPENDENCY_UNAVAILABLE" =>
                MediaErrors.DependencyUnavailable,

            "MEDIA.PERSISTENCE_ERROR" =>
                MediaErrors.PersistenceError,

            _ => MediaErrors.PersistenceError
        };
    }
}