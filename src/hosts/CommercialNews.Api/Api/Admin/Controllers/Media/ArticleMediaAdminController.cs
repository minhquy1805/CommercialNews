using CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.UseCases.ArticleMedia.AttachMediaToArticle;
using Media.Application.UseCases.ArticleMedia.DetachMediaFromArticle;
using Media.Application.UseCases.ArticleMedia.GetArticleMediaList;
using Media.Application.UseCases.ArticleMedia.GetArticlePrimaryMedia;
using Media.Application.UseCases.ArticleMedia.ReorderArticleMedia;
using Media.Application.UseCases.ArticleMedia.RestoreArticleMedia;
using Media.Application.UseCases.ArticleMedia.SetPrimaryMedia;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Media
{
    [ApiController]
    [Route("api/v1/admin/media/articles/{articleId:long}/attachments")]
    public sealed class ArticleMediaAdminController : ControllerBase
    {
        private readonly IAttachMediaToArticleUseCase _attachMediaToArticleUseCase;
        private readonly IDetachMediaFromArticleUseCase _detachMediaFromArticleUseCase;
        private readonly IRestoreArticleMediaUseCase _restoreArticleMediaUseCase;
        private readonly ISetPrimaryMediaUseCase _setPrimaryMediaUseCase;
        private readonly IReorderArticleMediaUseCase _reorderArticleMediaUseCase;
        private readonly IGetArticleMediaListUseCase _getArticleMediaListUseCase;
        private readonly IGetArticlePrimaryMediaUseCase _getArticlePrimaryMediaUseCase;

        public ArticleMediaAdminController(
            IAttachMediaToArticleUseCase attachMediaToArticleUseCase,
            IDetachMediaFromArticleUseCase detachMediaFromArticleUseCase,
            IRestoreArticleMediaUseCase restoreArticleMediaUseCase,
            ISetPrimaryMediaUseCase setPrimaryMediaUseCase,
            IReorderArticleMediaUseCase reorderArticleMediaUseCase,
            IGetArticleMediaListUseCase getArticleMediaListUseCase,
            IGetArticlePrimaryMediaUseCase getArticlePrimaryMediaUseCase)
        {
            _attachMediaToArticleUseCase = attachMediaToArticleUseCase;
            _detachMediaFromArticleUseCase = detachMediaFromArticleUseCase;
            _restoreArticleMediaUseCase = restoreArticleMediaUseCase;
            _setPrimaryMediaUseCase = setPrimaryMediaUseCase;
            _reorderArticleMediaUseCase = reorderArticleMediaUseCase;
            _getArticleMediaListUseCase = getArticleMediaListUseCase;
            _getArticlePrimaryMediaUseCase = getArticlePrimaryMediaUseCase;
        }

        [HttpPost]
        [ProducesResponseType(typeof(AttachMediaToArticleHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AttachAsync(
            [FromRoute] long articleId,
            [FromBody] AttachMediaToArticleHttpRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new AttachMediaToArticleRequest
            {
                ArticleId = articleId,
                MediaId = request.MediaId,
                IsPrimary = request.IsPrimary
            };

            Result<AttachMediaToArticleResponse> result =
                await _attachMediaToArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<AttachMediaToArticleHttpResponse>.Failure(result.Error!));
            }

            var response = new AttachMediaToArticleHttpResponse
            {
                ArticleMediaId = result.Value!.ArticleMediaId,
                ArticleId = result.Value.ArticleId,
                MediaId = result.Value.MediaId,
                Attached = result.Value.Attached,
                IsPrimary = result.Value.IsPrimary,
                AffectedRows = result.Value.AffectedRows
            };

            return this.ToActionResult(Result<AttachMediaToArticleHttpResponse>.Success(response));
        }

        [HttpPost("{mediaId:long}:detach")]
        [ProducesResponseType(typeof(DetachMediaFromArticleHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DetachAsync(
            [FromRoute] long articleId,
            [FromRoute] long mediaId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new DetachMediaFromArticleRequest
            {
                ArticleId = articleId,
                MediaId = mediaId
            };

            Result<DetachMediaFromArticleResponse> result =
                await _detachMediaFromArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<DetachMediaFromArticleHttpResponse>.Failure(result.Error!));
            }

            var response = new DetachMediaFromArticleHttpResponse
            {
                ArticleId = result.Value!.ArticleId,
                MediaId = result.Value.MediaId,
                Detached = result.Value.Detached,
                AffectedRows = result.Value.AffectedRows
            };

            return this.ToActionResult(Result<DetachMediaFromArticleHttpResponse>.Success(response));
        }

        [HttpPost("{mediaId:long}:restore")]
        [ProducesResponseType(typeof(RestoreArticleMediaHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RestoreAsync(
            [FromRoute] long articleId,
            [FromRoute] long mediaId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new RestoreArticleMediaRequest
            {
                ArticleId = articleId,
                MediaId = mediaId
            };

            Result<RestoreArticleMediaResponse> result =
                await _restoreArticleMediaUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<RestoreArticleMediaHttpResponse>.Failure(result.Error!));
            }

            var response = new RestoreArticleMediaHttpResponse
            {
                ArticleId = result.Value!.ArticleId,
                MediaId = result.Value.MediaId,
                Restored = result.Value.Restored,
                AffectedRows = result.Value.AffectedRows
            };

            return this.ToActionResult(Result<RestoreArticleMediaHttpResponse>.Success(response));
        }

        [HttpPost("{mediaId:long}:set-primary")]
        [ProducesResponseType(typeof(SetPrimaryMediaHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SetPrimaryAsync(
            [FromRoute] long articleId,
            [FromRoute] long mediaId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new SetPrimaryMediaRequest
            {
                ArticleId = articleId,
                MediaId = mediaId
            };

            Result<SetPrimaryMediaResponse> result =
                await _setPrimaryMediaUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<SetPrimaryMediaHttpResponse>.Failure(result.Error!));
            }

            var response = new SetPrimaryMediaHttpResponse
            {
                ArticleId = result.Value!.ArticleId,
                MediaId = result.Value.MediaId,
                PrimarySet = result.Value.PrimarySet,
                AffectedRows = result.Value.AffectedRows
            };

            return this.ToActionResult(Result<SetPrimaryMediaHttpResponse>.Success(response));
        }

        [HttpPost("reorder")]
        [ProducesResponseType(typeof(ReorderArticleMediaHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReorderAsync(
            [FromRoute] long articleId,
            [FromBody] ReorderArticleMediaHttpRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new ReorderArticleMediaRequest
            {
                ArticleId = articleId,
                Items = request.Items.Select(static item => new ReorderArticleMediaItemRequest
                {
                    MediaId = item.MediaId,
                    SortOrder = item.SortOrder
                }).ToArray()
            };

            Result<ReorderArticleMediaResponse> result =
                await _reorderArticleMediaUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<ReorderArticleMediaHttpResponse>.Failure(result.Error!));
            }

            var response = new ReorderArticleMediaHttpResponse
            {
                ArticleId = result.Value!.ArticleId,
                Reordered = result.Value.Reordered,
                AffectedRows = result.Value.AffectedRows
            };

            return this.ToActionResult(Result<ReorderArticleMediaHttpResponse>.Success(response));
        }

        [HttpGet]
        [ProducesResponseType(typeof(GetArticleMediaListHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPagedAsync(
            [FromRoute] long articleId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] string sortBy = "SortOrder",
            [FromQuery] string sortDirection = "ASC",
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new GetArticleMediaListRequest
            {
                ArticleId = articleId,
                Page = page,
                PageSize = pageSize,
                IncludeDeleted = includeDeleted,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            Result<GetArticleMediaListResponse> result =
                await _getArticleMediaListUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetArticleMediaListHttpResponse>.Failure(result.Error!));
            }

            var response = new GetArticleMediaListHttpResponse
            {
                Items = result.Value!.Items.Select(static item => new GetArticleMediaListItemHttpResponse
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
                }).ToArray(),
                Page = result.Value.Page,
                PageSize = result.Value.PageSize,
                TotalItems = result.Value.TotalItems
            };

            return this.ToActionResult(Result<GetArticleMediaListHttpResponse>.Success(response));
        }

        [HttpGet("primary")]
        [ProducesResponseType(typeof(GetArticlePrimaryMediaHttpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPrimaryAsync(
            [FromRoute] long articleId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new GetArticlePrimaryMediaRequest
            {
                ArticleId = articleId
            };

            Result<GetArticlePrimaryMediaResponse> result =
                await _getArticlePrimaryMediaUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetArticlePrimaryMediaHttpResponse>.Failure(result.Error!));
            }

            var response = new GetArticlePrimaryMediaHttpResponse
            {
                ArticleMediaId = result.Value!.ArticleMediaId,
                ArticleId = result.Value.ArticleId,
                MediaId = result.Value.MediaId,
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
                DefaultAltText = result.Value.DefaultAltText,
                AltTextOverride = result.Value.AltTextOverride,
                Caption = result.Value.Caption,
                SortOrder = result.Value.SortOrder,
                IsPrimary = result.Value.IsPrimary,
                CreatedAt = result.Value.CreatedAt,
                UpdatedAt = result.Value.UpdatedAt,
                Version = result.Value.Version
            };

            return this.ToActionResult(Result<GetArticlePrimaryMediaHttpResponse>.Success(response));
        }
    }
}