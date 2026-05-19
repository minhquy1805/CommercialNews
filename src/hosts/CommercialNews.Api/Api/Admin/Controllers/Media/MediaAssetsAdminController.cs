using CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;
using CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.UseCases.ArticleMedia.GetMediaUsage;
using Media.Application.UseCases.MediaAssets.GetMediaById;
using Media.Application.UseCases.MediaAssets.GetMediaByPublicId;
using Media.Application.UseCases.MediaAssets.GetMediaList;
using Media.Application.UseCases.MediaAssets.RegisterMedia;
using Media.Application.UseCases.MediaAssets.RestoreMedia;
using Media.Application.UseCases.MediaAssets.SoftDeleteMedia;
using Media.Application.UseCases.MediaAssets.UpdateMediaAsset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Media;

[ApiController]
[Route("api/v1/admin/media/items")]
public sealed class MediaAssetsAdminController : ControllerBase
{
    private const string GetMediaAssetByIdRouteName = "AdminMediaItems.GetById";

    private readonly IRegisterMediaUseCase _registerMediaUseCase;
    private readonly IGetMediaByIdUseCase _getMediaByIdUseCase;
    private readonly IGetMediaByPublicIdUseCase _getMediaByPublicIdUseCase;
    private readonly IGetMediaListUseCase _getMediaListUseCase;
    private readonly IUpdateMediaAssetUseCase _updateMediaAssetUseCase;
    private readonly ISoftDeleteMediaUseCase _softDeleteMediaUseCase;
    private readonly IRestoreMediaUseCase _restoreMediaUseCase;
    private readonly IGetMediaUsageUseCase _getMediaUsageUseCase;

    public MediaAssetsAdminController(
        IRegisterMediaUseCase registerMediaUseCase,
        IGetMediaByIdUseCase getMediaByIdUseCase,
        IGetMediaByPublicIdUseCase getMediaByPublicIdUseCase,
        IGetMediaListUseCase getMediaListUseCase,
        IUpdateMediaAssetUseCase updateMediaAssetUseCase,
        ISoftDeleteMediaUseCase softDeleteMediaUseCase,
        IRestoreMediaUseCase restoreMediaUseCase,
        IGetMediaUsageUseCase getMediaUsageUseCase)
    {
        _registerMediaUseCase = registerMediaUseCase
            ?? throw new ArgumentNullException(nameof(registerMediaUseCase));

        _getMediaByIdUseCase = getMediaByIdUseCase
            ?? throw new ArgumentNullException(nameof(getMediaByIdUseCase));

        _getMediaByPublicIdUseCase = getMediaByPublicIdUseCase
            ?? throw new ArgumentNullException(nameof(getMediaByPublicIdUseCase));

        _getMediaListUseCase = getMediaListUseCase
            ?? throw new ArgumentNullException(nameof(getMediaListUseCase));

        _updateMediaAssetUseCase = updateMediaAssetUseCase
            ?? throw new ArgumentNullException(nameof(updateMediaAssetUseCase));

        _softDeleteMediaUseCase = softDeleteMediaUseCase
            ?? throw new ArgumentNullException(nameof(softDeleteMediaUseCase));

        _restoreMediaUseCase = restoreMediaUseCase
            ?? throw new ArgumentNullException(nameof(restoreMediaUseCase));

        _getMediaUsageUseCase = getMediaUsageUseCase
            ?? throw new ArgumentNullException(nameof(getMediaUsageUseCase));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsCreate)]
    [ProducesResponseType(typeof(CreateMediaAssetHttpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateMediaAssetHttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var useCaseRequest = new RegisterMediaRequest
        {
            StorageProvider = request.StorageProvider,
            Url = request.Url,
            StoragePath = request.StoragePath,
            FileName = request.FileName,
            MediaType = request.MediaType,
            MimeType = request.MimeType,
            FileSizeBytes = request.FileSizeBytes,
            Width = request.Width,
            Height = request.Height,
            DurationSeconds = request.DurationSeconds,
            AltText = request.AltText,
            MetadataJson = request.MetadataJson,
            ContentHash = request.ContentHash
        };

        Result<RegisterMediaResponse> result =
            await _registerMediaUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<CreateMediaAssetHttpResponse>.Failure(result.Error!));
        }

        RegisterMediaResponse value = result.Value!;

        var response = new CreateMediaAssetHttpResponse
        {
            MediaId = value.MediaId,
            PublicId = value.PublicId,
            StorageProvider = value.StorageProvider,
            Url = value.Url,
            StoragePath = value.StoragePath,
            FileName = value.FileName,
            MediaType = value.MediaType,
            MimeType = value.MimeType,
            FileSizeBytes = value.FileSizeBytes,
            Width = value.Width,
            Height = value.Height,
            DurationSeconds = value.DurationSeconds,
            AltText = value.AltText,
            MetadataJson = value.MetadataJson,
            CreatedAt = value.CreatedAt,
            CreatedBy = value.CreatedBy,
            Version = value.Version
        };

        return CreatedAtRoute(
            GetMediaAssetByIdRouteName,
            new { mediaId = response.MediaId },
            response);
    }

    [HttpGet("{mediaId:long}", Name = GetMediaAssetByIdRouteName)]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsRead)]
    [ProducesResponseType(typeof(GetMediaAssetByIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] long mediaId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetMediaByIdRequest
        {
            MediaId = mediaId
        };

        Result<GetMediaByIdResponse> result =
            await _getMediaByIdUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetMediaAssetByIdHttpResponse>.Failure(result.Error!));
        }

        var response = MapByIdResponse(result.Value!);

        return this.ToActionResult(
            Result<GetMediaAssetByIdHttpResponse>.Success(response));
    }

    [HttpGet("public/{publicId}")]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsRead)]
    [ProducesResponseType(typeof(GetMediaAssetByPublicIdHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPublicIdAsync(
        [FromRoute] string publicId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetMediaByPublicIdRequest
        {
            PublicId = publicId
        };

        Result<GetMediaByPublicIdResponse> result =
            await _getMediaByPublicIdUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetMediaAssetByPublicIdHttpResponse>.Failure(result.Error!));
        }

        var response = MapByPublicIdResponse(result.Value!);

        return this.ToActionResult(
            Result<GetMediaAssetByPublicIdHttpResponse>.Success(response));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsRead)]
    [ProducesResponseType(typeof(GetMediaAssetsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] string? mediaType = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "DESC",
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetMediaListRequest
        {
            Page = page,
            PageSize = pageSize,
            IsDeleted = isDeleted,
            MediaType = mediaType,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        Result<GetMediaListResponse> result =
            await _getMediaListUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetMediaAssetsHttpResponse>.Failure(result.Error!));
        }

        GetMediaListResponse value = result.Value!;

        var response = new GetMediaAssetsHttpResponse
        {
            Items = value.Items
                .Select(MapListItem)
                .ToArray(),
            Page = value.Page,
            PageSize = value.PageSize,
            TotalItems = value.TotalItems
        };

        return this.ToActionResult(
            Result<GetMediaAssetsHttpResponse>.Success(response));
    }

    [HttpPatch("{mediaId:long}")]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsUpdate)]
    [ProducesResponseType(typeof(UpdateMediaAssetHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] long mediaId,
        [FromBody] UpdateMediaAssetHttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var useCaseRequest = new UpdateMediaAssetRequest
        {
            MediaId = mediaId,
            AltText = request.AltText,
            MetadataJson = request.MetadataJson
        };

        Result<UpdateMediaAssetResponse> result =
            await _updateMediaAssetUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<UpdateMediaAssetHttpResponse>.Failure(result.Error!));
        }

        UpdateMediaAssetResponse value = result.Value!;

        var response = new UpdateMediaAssetHttpResponse
        {
            MediaId = value.MediaId,
            PublicId = value.PublicId,
            AltText = value.AltText,
            MetadataJson = value.MetadataJson,
            UpdatedAt = value.UpdatedAt,
            UpdatedBy = value.UpdatedBy,
            Version = value.Version
        };

        return this.ToActionResult(
            Result<UpdateMediaAssetHttpResponse>.Success(response));
    }

    [HttpDelete("{mediaId:long}")]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsDelete)]
    [ProducesResponseType(typeof(SoftDeleteMediaAssetHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SoftDeleteAsync(
        [FromRoute] long mediaId,
        [FromBody] SoftDeleteMediaAssetHttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var useCaseRequest = new SoftDeleteMediaRequest
        {
            MediaId = mediaId,
            RestoreUntil = request.RestoreUntil
        };

        Result<SoftDeleteMediaResponse> result =
            await _softDeleteMediaUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<SoftDeleteMediaAssetHttpResponse>.Failure(result.Error!));
        }

        SoftDeleteMediaResponse value = result.Value!;

        var response = new SoftDeleteMediaAssetHttpResponse
        {
            MediaId = value.MediaId,
            PublicId = value.PublicId,
            IsDeleted = value.IsDeleted,
            DeletedAt = value.DeletedAt,
            DeletedBy = value.DeletedBy,
            RestoreUntil = value.RestoreUntil,
            AffectedRows = value.AffectedRows,
            PrimaryClearedCount = value.PrimaryClearedCount,
            Version = value.Version
        };

        return this.ToActionResult(
            Result<SoftDeleteMediaAssetHttpResponse>.Success(response));
    }

    [HttpPost("{mediaId:long}:restore")]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsRestore)]
    [ProducesResponseType(typeof(RestoreMediaAssetHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreAsync(
        [FromRoute] long mediaId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new RestoreMediaRequest
        {
            MediaId = mediaId
        };

        Result<RestoreMediaResponse> result =
            await _restoreMediaUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<RestoreMediaAssetHttpResponse>.Failure(result.Error!));
        }

        RestoreMediaResponse value = result.Value!;

        var response = new RestoreMediaAssetHttpResponse
        {
            MediaId = value.MediaId,
            PublicId = value.PublicId,
            IsRestored = value.IsRestored,
            IsDeleted = value.IsDeleted,
            RestoredAt = value.RestoredAt,
            RestoredBy = value.RestoredBy,
            AffectedRows = value.AffectedRows,
            Version = value.Version
        };

        return this.ToActionResult(
            Result<RestoreMediaAssetHttpResponse>.Success(response));
    }

    [HttpGet("{mediaId:long}/usages")]
    [Authorize(Policy = AuthorizationPolicies.MediaAssetsReadUsage)]
    [ProducesResponseType(typeof(GetMediaUsageHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsageAsync(
        [FromRoute] long mediaId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var useCaseRequest = new GetMediaUsageRequest
        {
            MediaId = mediaId,
            IncludeDeleted = includeDeleted
        };

        Result<GetMediaUsageResponse> result =
            await _getMediaUsageUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetMediaUsageHttpResponse>.Failure(result.Error!));
        }

        GetMediaUsageResponse value = result.Value!;

        var response = new GetMediaUsageHttpResponse
        {
            MediaId = value.MediaId,
            Items = value.Items
                .Select(static item => new GetMediaUsageItemHttpResponse
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
                })
                .ToArray()
        };

        return this.ToActionResult(
            Result<GetMediaUsageHttpResponse>.Success(response));
    }

    private static GetMediaAssetByIdHttpResponse MapByIdResponse(
        GetMediaByIdResponse value)
    {
        return new GetMediaAssetByIdHttpResponse
        {
            MediaId = value.MediaId,
            PublicId = value.PublicId,
            StorageProvider = value.StorageProvider,
            Url = value.Url,
            StoragePath = value.StoragePath,
            FileName = value.FileName,
            MediaType = value.MediaType,
            MimeType = value.MimeType,
            FileSizeBytes = value.FileSizeBytes,
            Width = value.Width,
            Height = value.Height,
            DurationSeconds = value.DurationSeconds,
            AltText = value.AltText,
            MetadataJson = value.MetadataJson,
            CreatedAt = value.CreatedAt,
            CreatedBy = value.CreatedBy,
            UpdatedAt = value.UpdatedAt,
            UpdatedBy = value.UpdatedBy,
            IsDeleted = value.IsDeleted,
            DeletedAt = value.DeletedAt,
            DeletedBy = value.DeletedBy,
            RestoreUntil = value.RestoreUntil,
            RestoredAt = value.RestoredAt,
            RestoredBy = value.RestoredBy,
            Version = value.Version
        };
    }

    private static GetMediaAssetByPublicIdHttpResponse MapByPublicIdResponse(
        GetMediaByPublicIdResponse value)
    {
        return new GetMediaAssetByPublicIdHttpResponse
        {
            MediaId = value.MediaId,
            PublicId = value.PublicId,
            StorageProvider = value.StorageProvider,
            Url = value.Url,
            StoragePath = value.StoragePath,
            FileName = value.FileName,
            MediaType = value.MediaType,
            MimeType = value.MimeType,
            FileSizeBytes = value.FileSizeBytes,
            Width = value.Width,
            Height = value.Height,
            DurationSeconds = value.DurationSeconds,
            AltText = value.AltText,
            MetadataJson = value.MetadataJson,
            CreatedAt = value.CreatedAt,
            CreatedBy = value.CreatedBy,
            UpdatedAt = value.UpdatedAt,
            UpdatedBy = value.UpdatedBy,
            IsDeleted = value.IsDeleted,
            DeletedAt = value.DeletedAt,
            DeletedBy = value.DeletedBy,
            RestoreUntil = value.RestoreUntil,
            RestoredAt = value.RestoredAt,
            RestoredBy = value.RestoredBy,
            Version = value.Version
        };
    }

    private static GetMediaAssetsItemHttpResponse MapListItem(
        GetMediaListItemResponse item)
    {
        return new GetMediaAssetsItemHttpResponse
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
}