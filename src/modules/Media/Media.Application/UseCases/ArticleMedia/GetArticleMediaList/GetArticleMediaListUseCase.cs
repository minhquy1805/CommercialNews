using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Errors;
using Media.Application.Models.Queries;
using Media.Application.Models.Results;
using Media.Application.Ports.Persistence;
using Media.Domain.Exceptions;

namespace Media.Application.UseCases.ArticleMedia.GetArticleMediaList;

public sealed class GetArticleMediaListUseCase : IGetArticleMediaListUseCase
{
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SortOrder",
            "CreatedAt",
            "UpdatedAt",
            "MediaType",
            "FileName"
        };

    private readonly IArticleMediaRepository _articleMediaRepository;

    public GetArticleMediaListUseCase(
        IArticleMediaRepository articleMediaRepository)
    {
        _articleMediaRepository = articleMediaRepository
            ?? throw new ArgumentNullException(nameof(articleMediaRepository));
    }

    public async Task<Result<GetArticleMediaListResponse>> ExecuteAsync(
        GetArticleMediaListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetArticleMediaListResponse>.Failure(
                    MediaErrors.ArticleMedia.InvalidArticleId);
            }

            if (request.Page <= 0 || request.PageSize <= 0)
            {
                return Result<GetArticleMediaListResponse>.Failure(
                    MediaErrors.ValidationFailed);
            }

            if (request.PageSize > MaxPageSize)
            {
                return Result<GetArticleMediaListResponse>.Failure(
                    MediaErrors.ValidationFailed);
            }

            ArticleMediaListQuery query = new()
            {
                ArticleId = request.ArticleId,
                Page = request.Page,
                PageSize = request.PageSize,
                IncludeDeleted = request.IncludeDeleted,
                SortBy = NormalizeSortBy(request.SortBy),
                SortDirection = NormalizeSortDirection(request.SortDirection)
            };

            var pagedResult = await _articleMediaRepository.SelectByArticleIdAsync(
                query,
                cancellationToken);

            return Result<GetArticleMediaListResponse>.Success(
                new GetArticleMediaListResponse
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
            return Result<GetArticleMediaListResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (MediaDomainException exception)
        {
            return Result<GetArticleMediaListResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static GetArticleMediaListItemResponse MapItem(
        ArticleMediaListResultItem item)
    {
        return new GetArticleMediaListItemResponse
        {
            ArticleMediaId = item.ArticleMediaId,

            ArticleId = item.ArticleId,
            AttachmentSetVersion = item.AttachmentSetVersion,

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
            MediaIsDeleted = item.MediaIsDeleted,

            AltTextOverride = item.AltTextOverride,
            Caption = item.Caption,

            SortOrder = item.SortOrder,
            IsPrimary = item.IsPrimary,

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

    private static string NormalizeSortBy(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return "SortOrder";
        }

        string normalized = sortBy.Trim();

        return AllowedSortFields.Contains(normalized)
            ? normalized
            : "SortOrder";
    }

    private static string NormalizeSortDirection(string? sortDirection)
    {
        if (string.Equals(sortDirection, "DESC", StringComparison.OrdinalIgnoreCase))
        {
            return "DESC";
        }

        return "ASC";
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