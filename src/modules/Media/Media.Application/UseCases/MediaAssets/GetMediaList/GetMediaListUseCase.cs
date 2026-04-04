using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Models.QueryModels;
using Media.Application.Ports.Persistence;
using Media.Domain.Enums;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.GetMediaList;

public sealed class GetMediaListUseCase : IGetMediaListUseCase
{
    private readonly IMediaAssetRepository _mediaAssetRepository;

    public GetMediaListUseCase(IMediaAssetRepository mediaAssetRepository)
    {
        _mediaAssetRepository = mediaAssetRepository;
    }

    public async Task<Result<GetMediaListResponse>> ExecuteAsync(
        GetMediaListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Page <= 0 || request.PageSize <= 0)
            {
                return Result<GetMediaListResponse>.Failure(MediaErrors.ValidationFailed);
            }

            if (!string.IsNullOrWhiteSpace(request.MediaType) &&
                !MediaTypes.IsValid(request.MediaType))
            {
                return Result<GetMediaListResponse>.Failure(MediaErrors.MediaAsset.TypeNotAllowed);
            }

            MediaAssetListQuery query = new()
            {
                Page = request.Page,
                PageSize = request.PageSize,
                IsDeleted = request.IsDeleted,
                MediaType = request.MediaType,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            var pagedResult = await _mediaAssetRepository.SelectSkipAndTakeAsync(
                query,
                cancellationToken);

            return Result<GetMediaListResponse>.Success(new GetMediaListResponse
            {
                Items = pagedResult.Items
                    .Select(static item => new GetMediaListItemResponse
                    {
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
                        AltText = item.AltText,
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
            return Result<GetMediaListResponse>.Failure(MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetMediaListResponse>.Failure(MapDomainException(exception));
        }
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.TYPE_NOT_ALLOWED" => MediaErrors.MediaAsset.TypeNotAllowed,
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