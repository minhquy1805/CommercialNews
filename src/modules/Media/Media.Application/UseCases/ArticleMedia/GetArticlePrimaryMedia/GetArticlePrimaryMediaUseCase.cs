using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.GetArticlePrimaryMedia;

public sealed class GetArticlePrimaryMediaUseCase : IGetArticlePrimaryMediaUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;

    public GetArticlePrimaryMediaUseCase(IArticleMediaRepository articleMediaRepository)
    {
        _articleMediaRepository = articleMediaRepository;
    }

    public async Task<Result<GetArticlePrimaryMediaResponse>> ExecuteAsync(
        GetArticlePrimaryMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetArticlePrimaryMediaResponse>.Failure(MediaErrors.ArticleMedia.InvalidArticleId);
            }

            var primaryMedia = await _articleMediaRepository.GetPrimaryByArticleIdAsync(
                request.ArticleId,
                cancellationToken);

            if (primaryMedia is null)
            {
                return Result<GetArticlePrimaryMediaResponse>.Failure(MediaErrors.ArticleMedia.NotFound);
            }

            return Result<GetArticlePrimaryMediaResponse>.Success(new GetArticlePrimaryMediaResponse
            {
                ArticleMediaId = primaryMedia.ArticleMediaId,
                ArticleId = primaryMedia.ArticleId,
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
                AltTextOverride = primaryMedia.AltTextOverride,
                Caption = primaryMedia.Caption,
                SortOrder = primaryMedia.SortOrder,
                IsPrimary = primaryMedia.IsPrimary,
                CreatedAt = primaryMedia.CreatedAt,
                UpdatedAt = primaryMedia.UpdatedAt,
                Version = primaryMedia.Version
            });
        }
        catch (PersistenceException exception)
        {
            return Result<GetArticlePrimaryMediaResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetArticlePrimaryMediaResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.ARTICLE_MEDIA_INVALID_ARTICLE_ID" => MediaErrors.ArticleMedia.InvalidArticleId,
            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            _ => MediaErrors.ValidationFailed
        };
    }
}