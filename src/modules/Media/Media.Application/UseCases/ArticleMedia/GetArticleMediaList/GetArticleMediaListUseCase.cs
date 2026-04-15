using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Models.QueryModels;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.GetArticleMediaList;

public sealed class GetArticleMediaListUseCase : IGetArticleMediaListUseCase
{
    private readonly IArticleMediaRepository _articleMediaRepository;

    public GetArticleMediaListUseCase(IArticleMediaRepository articleMediaRepository)
    {
        _articleMediaRepository = articleMediaRepository;
    }

    public async Task<Result<GetArticleMediaListResponse>> ExecuteAsync(
        GetArticleMediaListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetArticleMediaListResponse>.Failure(MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.Page <= 0 || request.PageSize <= 0)
            {
                return Result<GetArticleMediaListResponse>.Failure(MediaErrors.ValidationFailed);
            }

            ArticleMediaListQuery query = new()
            {
                ArticleId = request.ArticleId,
                Page = request.Page,
                PageSize = request.PageSize,
                IncludeDeleted = request.IncludeDeleted,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            var pagedResult = await _articleMediaRepository.SelectByArticleIdAsync(
                query,
                cancellationToken);

            return Result<GetArticleMediaListResponse>.Success(new GetArticleMediaListResponse
            {
                Items = pagedResult.Items
                    .Select(static item => new GetArticleMediaListItemResponse
                    {
                        ArticleMediaId = item.ArticleMediaId,
                        ArticleId = item.ArticleId,
                        MediaId = item.MediaId,
                        PublicId = item.PublicId,
                        StorageProvider = item.StorageProvider,
                        Url = item.Url,
                        StoragePath = item.StoragePath,
                        FileName = item.FileName,
                        MediaType = item.MediaType,
                        MimeType = item.MimeType,
                        FileSizeBytes = item.FileSizeBytes,
                        Width = item.Width,
                        Height = item.Height,
                        DurationSeconds = item.DurationSeconds,
                        DefaultAltText = item.DefaultAltText,
                        AltTextOverride = item.AltTextOverride,
                        Caption = item.Caption,
                        SortOrder = item.SortOrder,
                        IsPrimary = item.IsPrimary,
                        CreatedAt = item.CreatedAt,
                        UpdatedAt = item.UpdatedAt,
                        IsDeleted = item.IsDeleted,
                        Version = item.Version
                    })
                    .ToArray(),
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalItems = pagedResult.TotalItems
            });
        }
        catch (PersistenceException exception)
        {
            return Result<GetArticleMediaListResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetArticleMediaListResponse>.Failure(MapDomainException(exception));
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