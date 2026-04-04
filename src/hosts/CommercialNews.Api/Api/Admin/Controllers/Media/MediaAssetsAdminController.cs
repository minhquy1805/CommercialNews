using CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Media.MediaAssets.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.MediaAsset.Requests;
using Media.Application.Contracts.MediaAsset.Responses;
using Media.Application.UseCases.MediaAssets.GetMediaById;
using Media.Application.UseCases.MediaAssets.GetMediaByPublicId;
using Media.Application.UseCases.MediaAssets.GetMediaList;
using Media.Application.UseCases.MediaAssets.RegisterMedia;
using Media.Application.UseCases.MediaAssets.RestoreMedia;
using Media.Application.UseCases.MediaAssets.SoftDeleteMedia;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Media
{
    [ApiController]
    [Route("api/v1/admin/media/assets")]
    public sealed class MediaAssetsAdminController : ControllerBase
    {
        private const string GetMediaAssetByIdRouteName = "AdminMediaAssets.GetById";

        private readonly IRegisterMediaUseCase _registerMediaUseCase;
        private readonly IGetMediaByIdUseCase _getMediaByIdUseCase;
        private readonly IGetMediaByPublicIdUseCase _getMediaByPublicIdUseCase;
        private readonly IGetMediaListUseCase _getMediaListUseCase;
        private readonly ISoftDeleteMediaUseCase _softDeleteMediaUseCase;
        private readonly IRestoreMediaUseCase _restoreMediaUseCase;

        public MediaAssetsAdminController(
            IRegisterMediaUseCase registerMediaUseCase,
            IGetMediaByIdUseCase getMediaByIdUseCase,
            IGetMediaByPublicIdUseCase getMediaByPublicIdUseCase,
            IGetMediaListUseCase getMediaListUseCase,
            ISoftDeleteMediaUseCase softDeleteMediaUseCase,
            IRestoreMediaUseCase restoreMediaUseCase)
        {
            _registerMediaUseCase = registerMediaUseCase;
            _getMediaByIdUseCase = getMediaByIdUseCase;
            _getMediaByPublicIdUseCase = getMediaByPublicIdUseCase;
            _getMediaListUseCase = getMediaListUseCase;
            _softDeleteMediaUseCase = softDeleteMediaUseCase;
            _restoreMediaUseCase = restoreMediaUseCase;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateMediaAssetHttpResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateMediaAssetHttpRequest request,
            CancellationToken cancellationToken)
        {
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
                MetadataJson = request.MetadataJson
            };

            Result<RegisterMediaResponse> result =
                await _registerMediaUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<CreateMediaAssetHttpResponse>.Failure(result.Error!));
            }

            var response = new CreateMediaAssetHttpResponse
            {
                MediaId = result.Value!.MediaId,
                PublicId = result.Value.PublicId,
                StorageProvider = result.Value.StorageProvider,
                Url = result.Value.Url,
                StoragePath = result.Value.StoragePath,
                FileName = result.Value.FileName,
                MediaType = result.Value.MediaType,
                MimeType = result.Value.MimeType,
                FileSizeBytes = result.Value.FileSizeBytes,
                Width = result.Value.Width,
                Height = result.Value.Height,
                DurationSeconds = result.Value.DurationSeconds,
                AltText = result.Value.AltText,
                MetadataJson = result.Value.MetadataJson,
                CreatedAt = result.Value.CreatedAt,
                CreatedByUserId = result.Value.CreatedByUserId,
                Version = result.Value.Version
            };

            return CreatedAtRoute(
                GetMediaAssetByIdRouteName,
                new { mediaId = response.MediaId },
                response);
        }

        [HttpGet("{mediaId:long}", Name = GetMediaAssetByIdRouteName)]
        [ProducesResponseType(typeof(GetMediaAssetByIdHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
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
                await _getMediaByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetMediaAssetByIdHttpResponse>.Failure(result.Error!));
            }

            var response = new GetMediaAssetByIdHttpResponse
            {
                MediaId = result.Value!.MediaId,
                PublicId = result.Value.PublicId,
                StorageProvider = result.Value.StorageProvider,
                Url = result.Value.Url,
                StoragePath = result.Value.StoragePath,
                FileName = result.Value.FileName,
                MediaType = result.Value.MediaType,
                MimeType = result.Value.MimeType,
                FileSizeBytes = result.Value.FileSizeBytes,
                Width = result.Value.Width,
                Height = result.Value.Height,
                DurationSeconds = result.Value.DurationSeconds,
                AltText = result.Value.AltText,
                MetadataJson = result.Value.MetadataJson,
                CreatedAt = result.Value.CreatedAt,
                CreatedByUserId = result.Value.CreatedByUserId,
                UpdatedAt = result.Value.UpdatedAt,
                UpdatedByUserId = result.Value.UpdatedByUserId,
                IsDeleted = result.Value.IsDeleted,
                DeletedAt = result.Value.DeletedAt,
                DeletedByUserId = result.Value.DeletedByUserId,
                RestoreUntil = result.Value.RestoreUntil,
                Version = result.Value.Version
            };

            return this.ToActionResult(Result<GetMediaAssetByIdHttpResponse>.Success(response));
        }

        [HttpGet("public/{publicId}")]
        [ProducesResponseType(typeof(GetMediaAssetByPublicIdHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
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
                await _getMediaByPublicIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetMediaAssetByPublicIdHttpResponse>.Failure(result.Error!));
            }

            var response = new GetMediaAssetByPublicIdHttpResponse
            {
                MediaId = result.Value!.MediaId,
                PublicId = result.Value.PublicId,
                StorageProvider = result.Value.StorageProvider,
                Url = result.Value.Url,
                StoragePath = result.Value.StoragePath,
                FileName = result.Value.FileName,
                MediaType = result.Value.MediaType,
                MimeType = result.Value.MimeType,
                FileSizeBytes = result.Value.FileSizeBytes,
                Width = result.Value.Width,
                Height = result.Value.Height,
                DurationSeconds = result.Value.DurationSeconds,
                AltText = result.Value.AltText,
                MetadataJson = result.Value.MetadataJson,
                CreatedAt = result.Value.CreatedAt,
                CreatedByUserId = result.Value.CreatedByUserId,
                UpdatedAt = result.Value.UpdatedAt,
                UpdatedByUserId = result.Value.UpdatedByUserId,
                IsDeleted = result.Value.IsDeleted,
                DeletedAt = result.Value.DeletedAt,
                DeletedByUserId = result.Value.DeletedByUserId,
                RestoreUntil = result.Value.RestoreUntil,
                Version = result.Value.Version
            };

            return this.ToActionResult(Result<GetMediaAssetByPublicIdHttpResponse>.Success(response));
        }

        [HttpGet]
        [ProducesResponseType(typeof(GetMediaAssetsHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
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
                await _getMediaListUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetMediaAssetsHttpResponse>.Failure(result.Error!));
            }

            var response = new GetMediaAssetsHttpResponse
            {
                Items = result.Value!.Items.Select(static item => new GetMediaAssetsItemHttpResponse
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
                }).ToArray(),
                Page = result.Value.Page,
                PageSize = result.Value.PageSize,
                TotalItems = result.Value.TotalItems
            };

            return this.ToActionResult(Result<GetMediaAssetsHttpResponse>.Success(response));
        }

        [HttpPost("{mediaId:long}:soft-delete")]
        [ProducesResponseType(typeof(SoftDeleteMediaAssetHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SoftDeleteAsync(
            [FromRoute] long mediaId,
            [FromBody] SoftDeleteMediaAssetHttpRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new SoftDeleteMediaRequest
            {
                MediaId = mediaId,
                RestoreUntil = request.RestoreUntil
            };

            Result<SoftDeleteMediaResponse> result =
                await _softDeleteMediaUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<SoftDeleteMediaAssetHttpResponse>.Failure(result.Error!));
            }

            var response = new SoftDeleteMediaAssetHttpResponse
            {
                MediaId = result.Value!.MediaId,
                IsDeleted = result.Value.IsDeleted,
                RestoreUntil = result.Value.RestoreUntil,
                AffectedRows = result.Value.AffectedRows
            };

            return this.ToActionResult(Result<SoftDeleteMediaAssetHttpResponse>.Success(response));
        }

        [HttpPost("{mediaId:long}:restore")]
        [ProducesResponseType(typeof(RestoreMediaAssetHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
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
                await _restoreMediaUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<RestoreMediaAssetHttpResponse>.Failure(result.Error!));
            }

            var response = new RestoreMediaAssetHttpResponse
            {
                MediaId = result.Value!.MediaId,
                IsRestored = result.Value.IsRestored,
                AffectedRows = result.Value.AffectedRows
            };

            return this.ToActionResult(Result<RestoreMediaAssetHttpResponse>.Success(response));
        }
    }
}