using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.Errors;
using Media.Application.Models.Queries;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Domain.Constants;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.MediaAssets.GetMediaList;

public sealed class GetMediaListUseCase : IGetMediaListUseCase
{
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt",
            "UpdatedAt",
            "MediaType",
            "FileName",
            "FileSizeBytes"
        };

    private readonly IMediaAssetRepository _mediaAssetRepository;

    public GetMediaListUseCase(IMediaAssetRepository mediaAssetRepository)
    {
        _mediaAssetRepository = mediaAssetRepository
            ?? throw new ArgumentNullException(nameof(mediaAssetRepository));
    }

    public async Task<Result<GetMediaListResponse>> ExecuteAsync(
        GetMediaListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.Page <= 0 || request.PageSize <= 0)
            {
                return Result<GetMediaListResponse>.Failure(
                    MediaErrors.ValidationFailed);
            }

            if (request.PageSize > MaxPageSize)
            {
                return Result<GetMediaListResponse>.Failure(
                    MediaErrors.ValidationFailed);
            }

            string? mediaType = NormalizeOptional(request.MediaType);

            if (mediaType is not null && !MediaTypes.IsValid(mediaType))
            {
                return Result<GetMediaListResponse>.Failure(
                    MediaErrors.MediaAsset.TypeNotAllowed);
            }

            string sortBy = NormalizeSortBy(request.SortBy);
            string sortDirection = NormalizeSortDirection(request.SortDirection);

            MediaAssetListQuery query = new()
            {
                Page = request.Page,
                PageSize = request.PageSize,
                IsDeleted = request.IsDeleted,
                MediaType = mediaType,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var pagedResult = await _mediaAssetRepository.SelectSkipAndTakeAsync(
                query,
                cancellationToken);

            return Result<GetMediaListResponse>.Success(
                new GetMediaListResponse
                {
                    Items = pagedResult.Items
                        .Select(MapItem)
                        .ToArray(),
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalItems = pagedResult.TotalItems
                });
        }
        catch (PersistenceException exception)
        {
            return Result<GetMediaListResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetMediaListResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static GetMediaListItemResponse MapItem(
        MediaAssetListResultItem item)
    {
        return new GetMediaListItemResponse
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
            MetadataJson = item.MetadataJson,

            Version = item.Version,

            CreatedAt = item.CreatedAt,
            CreatedBy = item.CreatedBy,

            UpdatedAt = item.UpdatedAt,
            UpdatedBy = item.UpdatedBy,

            IsDeleted = item.IsDeleted,
            DeletedAt = item.DeletedAt,
            DeletedBy = item.DeletedBy,
            RestoreUntil = item.RestoreUntil,

            RestoredAt = item.RestoredAt,
            RestoredBy = item.RestoredBy
        };
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return "CreatedAt";
        }

        string normalized = sortBy.Trim();

        return AllowedSortFields.Contains(normalized)
            ? normalized
            : "CreatedAt";
    }

    private static string NormalizeSortDirection(string? sortDirection)
    {
        if (string.Equals(sortDirection, "ASC", StringComparison.OrdinalIgnoreCase))
        {
            return "ASC";
        }

        return "DESC";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static Error MapDomainException(MediaDomainException exception)
    {
        return exception.Code switch
        {
            "MEDIA.TYPE_NOT_ALLOWED" =>
                MediaErrors.MediaAsset.TypeNotAllowed,

            _ => MediaErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "MEDIA.TYPE_NOT_ALLOWED" =>
                MediaErrors.MediaAsset.TypeNotAllowed,

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