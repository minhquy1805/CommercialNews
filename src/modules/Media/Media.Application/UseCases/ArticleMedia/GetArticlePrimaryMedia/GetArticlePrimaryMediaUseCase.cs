using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.GetArticlePrimaryMedia;

public sealed class GetArticlePrimaryMediaUseCase : IGetArticlePrimaryMediaUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;

    public GetArticlePrimaryMediaUseCase(
        IArticleMediaRepository articleMediaRepository)
    {
        _articleMediaRepository = articleMediaRepository
            ?? throw new ArgumentNullException(nameof(articleMediaRepository));
    }

    public async Task<Result<GetArticlePrimaryMediaResponse>> ExecuteAsync(
        GetArticlePrimaryMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetArticlePrimaryMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidArticleId);
            }

            ArticleMediaListResultItem? primaryMedia =
                await _articleMediaRepository.GetPrimaryByArticleIdAsync(
                    request.ArticleId,
                    cancellationToken);

            if (primaryMedia is null)
            {
                return Result<GetArticlePrimaryMediaResponse>.Failure(
                    MediaErrors.ArticleMedia.NotFound);
            }

            return Result<GetArticlePrimaryMediaResponse>.Success(
                MapResponse(primaryMedia));
        }
        catch (PersistenceException exception)
        {
            return Result<GetArticlePrimaryMediaResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetArticlePrimaryMediaResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static GetArticlePrimaryMediaResponse MapResponse(
        ArticleMediaListResultItem primaryMedia)
    {
        return new GetArticlePrimaryMediaResponse
        {
            ArticleMediaId = primaryMedia.ArticleMediaId,

            ArticleId = primaryMedia.ArticleId,
            AttachmentSetVersion = primaryMedia.AttachmentSetVersion,

            MediaId = primaryMedia.MediaId,
            PublicId = primaryMedia.PublicId,

            StorageProvider = primaryMedia.StorageProvider,
            Url = primaryMedia.Url,
            StoragePath = primaryMedia.StoragePath,
            FileName = primaryMedia.FileName,

            MediaType = primaryMedia.MediaType,
            MimeType = primaryMedia.MimeType,

            FileSizeBytes = primaryMedia.FileSizeBytes,
            Width = primaryMedia.Width,
            Height = primaryMedia.Height,
            DurationSeconds = primaryMedia.DurationSeconds,

            DefaultAltText = primaryMedia.DefaultAltText,
            MediaIsDeleted = primaryMedia.MediaIsDeleted,

            AltTextOverride = primaryMedia.AltTextOverride,
            Caption = primaryMedia.Caption,

            SortOrder = primaryMedia.SortOrder,
            IsPrimary = primaryMedia.IsPrimary,

            CreatedAt = primaryMedia.CreatedAt,
            CreatedBy = primaryMedia.CreatedBy,

            UpdatedAt = primaryMedia.UpdatedAt,
            UpdatedBy = primaryMedia.UpdatedBy,

            Version = primaryMedia.Version,

            IsDeleted = primaryMedia.IsDeleted,
            DeletedAt = primaryMedia.DeletedAt,
            DeletedBy = primaryMedia.DeletedBy
        };
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" =>
                MediaErrors.ArticleMedia.InvalidArticleId,

            "MEDIA.ARTICLE_MEDIA_SET_INVALID_ARTICLE_ID" =>
                MediaErrors.ArticleMediaSet.InvalidArticleId,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_NOT_FOUND" =>
                MediaErrors.Article.NotFound,

            "MEDIA.ATTACHMENT_NOT_FOUND" =>
                MediaErrors.ArticleMedia.NotFound,

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