using CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Media.ArticleMedia.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;
using Media.Application.UseCases.ArticleMedia.AttachMediaToArticle;
using Media.Application.UseCases.ArticleMedia.DetachMediaFromArticle;
using Media.Application.UseCases.ArticleMedia.GetArticleMediaList;
using Media.Application.UseCases.ArticleMedia.GetArticleMediaSet;
using Media.Application.UseCases.ArticleMedia.GetArticlePrimaryMedia;
using Media.Application.UseCases.ArticleMedia.ReorderArticleMedia;
using Media.Application.UseCases.ArticleMedia.SetPrimaryMedia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Media;

[ApiController]
[Route("api/v1/admin/media/articles/{articleId:long}")]
public sealed class ArticleMediaAdminController : ControllerBase
{
    private readonly IAttachMediaToArticleUseCase _attachMediaToArticleUseCase;
    private readonly IDetachMediaFromArticleUseCase _detachMediaFromArticleUseCase;
    private readonly ISetPrimaryMediaUseCase _setPrimaryMediaUseCase;
    private readonly IReorderArticleMediaUseCase _reorderArticleMediaUseCase;
    private readonly IGetArticleMediaListUseCase _getArticleMediaListUseCase;
    private readonly IGetArticlePrimaryMediaUseCase _getArticlePrimaryMediaUseCase;
    private readonly IGetArticleMediaSetUseCase _getArticleMediaSetUseCase;

    public ArticleMediaAdminController(
        IAttachMediaToArticleUseCase attachMediaToArticleUseCase,
        IDetachMediaFromArticleUseCase detachMediaFromArticleUseCase,
        ISetPrimaryMediaUseCase setPrimaryMediaUseCase,
        IReorderArticleMediaUseCase reorderArticleMediaUseCase,
        IGetArticleMediaListUseCase getArticleMediaListUseCase,
        IGetArticlePrimaryMediaUseCase getArticlePrimaryMediaUseCase,
        IGetArticleMediaSetUseCase getArticleMediaSetUseCase)
    {
        _attachMediaToArticleUseCase = attachMediaToArticleUseCase
            ?? throw new ArgumentNullException(nameof(attachMediaToArticleUseCase));

        _detachMediaFromArticleUseCase = detachMediaFromArticleUseCase
            ?? throw new ArgumentNullException(nameof(detachMediaFromArticleUseCase));

        _setPrimaryMediaUseCase = setPrimaryMediaUseCase
            ?? throw new ArgumentNullException(nameof(setPrimaryMediaUseCase));

        _reorderArticleMediaUseCase = reorderArticleMediaUseCase
            ?? throw new ArgumentNullException(nameof(reorderArticleMediaUseCase));

        _getArticleMediaListUseCase = getArticleMediaListUseCase
            ?? throw new ArgumentNullException(nameof(getArticleMediaListUseCase));

        _getArticlePrimaryMediaUseCase = getArticlePrimaryMediaUseCase
            ?? throw new ArgumentNullException(nameof(getArticlePrimaryMediaUseCase));

        _getArticleMediaSetUseCase = getArticleMediaSetUseCase
            ?? throw new ArgumentNullException(nameof(getArticleMediaSetUseCase));
    }

    [HttpPost("attachments")]
    [Authorize(Policy = AuthorizationPolicies.MediaArticleMediaAttach)]
    [ProducesResponseType(typeof(AttachMediaToArticleHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AttachAsync(
        [FromRoute] long articleId,
        [FromBody] AttachMediaToArticleHttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var useCaseRequest = new AttachMediaToArticleRequest
        {
            ArticleId = articleId,
            MediaId = request.MediaId,
            IsPrimary = request.IsPrimary
        };

        Result<AttachMediaToArticleResponse> result =
            await _attachMediaToArticleUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<AttachMediaToArticleHttpResponse>.Failure(result.Error!));
        }

        AttachMediaToArticleResponse value = result.Value!;

        var response = new AttachMediaToArticleHttpResponse
        {
            ArticleMediaId = value.ArticleMediaId,
            ArticleId = value.ArticleId,
            MediaId = value.MediaId,
            Attached = value.Attached,
            IsPrimary = value.IsPrimary,
            PrimaryChanged = value.PrimaryChanged,
            AffectedRows = value.AffectedRows,
            AttachmentSetVersion = value.AttachmentSetVersion
        };

        return this.ToActionResult(
            Result<AttachMediaToArticleHttpResponse>.Success(response));
    }

    [HttpDelete("attachments/{mediaId:long}")]
    [Authorize(Policy = AuthorizationPolicies.MediaArticleMediaDetach)]
    [ProducesResponseType(typeof(DetachMediaFromArticleHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
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
            await _detachMediaFromArticleUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<DetachMediaFromArticleHttpResponse>.Failure(result.Error!));
        }

        DetachMediaFromArticleResponse value = result.Value!;

        var response = new DetachMediaFromArticleHttpResponse
        {
            ArticleId = value.ArticleId,
            MediaId = value.MediaId,
            Detached = value.Detached,
            PrimaryCleared = value.PrimaryCleared,
            AffectedRows = value.AffectedRows,
            AttachmentSetVersion = value.AttachmentSetVersion
        };

        return this.ToActionResult(
            Result<DetachMediaFromArticleHttpResponse>.Success(response));
    }

    [HttpPost("attachments:set-primary")]
    [Authorize(Policy = AuthorizationPolicies.MediaArticleMediaSetPrimary)]
    [ProducesResponseType(typeof(SetPrimaryMediaHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetPrimaryAsync(
        [FromRoute] long articleId,
        [FromBody] SetPrimaryMediaHttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var useCaseRequest = new SetPrimaryMediaRequest
        {
            ArticleId = articleId,
            MediaId = request.MediaId,
            ExpectedVersion = request.ExpectedVersion
        };

        Result<SetPrimaryMediaResponse> result =
            await _setPrimaryMediaUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<SetPrimaryMediaHttpResponse>.Failure(result.Error!));
        }

        SetPrimaryMediaResponse value = result.Value!;

        var response = new SetPrimaryMediaHttpResponse
        {
            ArticleId = value.ArticleId,
            MediaId = value.MediaId,
            PrimarySet = value.PrimarySet,
            AffectedRows = value.AffectedRows,
            AttachmentSetVersion = value.AttachmentSetVersion
        };

        return this.ToActionResult(
            Result<SetPrimaryMediaHttpResponse>.Success(response));
    }

    [HttpPost("attachments:reorder")]
    [Authorize(Policy = AuthorizationPolicies.MediaArticleMediaReorder)]
    [ProducesResponseType(typeof(ReorderArticleMediaHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderAsync(
        [FromRoute] long articleId,
        [FromBody] ReorderArticleMediaHttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var useCaseRequest = new ReorderArticleMediaRequest
        {
            ArticleId = articleId,
            ExpectedVersion = request.ExpectedVersion,
            Items = request.Items
                .Select(static item => new ReorderArticleMediaItemRequest
                {
                    MediaId = item.MediaId,
                    SortOrder = item.SortOrder
                })
                .ToArray()
        };

        Result<ReorderArticleMediaResponse> result =
            await _reorderArticleMediaUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<ReorderArticleMediaHttpResponse>.Failure(result.Error!));
        }

        ReorderArticleMediaResponse value = result.Value!;

        var response = new ReorderArticleMediaHttpResponse
        {
            ArticleId = value.ArticleId,
            Reordered = value.Reordered,
            AffectedRows = value.AffectedRows,
            AttachmentSetVersion = value.AttachmentSetVersion
        };

        return this.ToActionResult(
            Result<ReorderArticleMediaHttpResponse>.Success(response));
    }

    [HttpGet("attachments")]
    [Authorize(Policy = AuthorizationPolicies.MediaArticleMediaRead)]
    [ProducesResponseType(typeof(GetArticleMediaListHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
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
            await _getArticleMediaListUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetArticleMediaListHttpResponse>.Failure(result.Error!));
        }

        GetArticleMediaListResponse value = result.Value!;

        var response = new GetArticleMediaListHttpResponse
        {
            Items = value.Items
                .Select(MapListItem)
                .ToArray(),
            Page = value.Page,
            PageSize = value.PageSize,
            TotalItems = value.TotalItems
        };

        return this.ToActionResult(
            Result<GetArticleMediaListHttpResponse>.Success(response));
    }

    [HttpGet("attachments/primary")]
    [Authorize(Policy = AuthorizationPolicies.MediaArticleMediaRead)]
    [ProducesResponseType(typeof(GetArticlePrimaryMediaHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
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
            await _getArticlePrimaryMediaUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetArticlePrimaryMediaHttpResponse>.Failure(result.Error!));
        }

        GetArticlePrimaryMediaResponse value = result.Value!;

        var response = new GetArticlePrimaryMediaHttpResponse
        {
            ArticleMediaId = value.ArticleMediaId,
            ArticleId = value.ArticleId,
            AttachmentSetVersion = value.AttachmentSetVersion,
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
            DefaultAltText = value.DefaultAltText,
            MediaIsDeleted = value.MediaIsDeleted,
            AltTextOverride = value.AltTextOverride,
            Caption = value.Caption,
            SortOrder = value.SortOrder,
            IsPrimary = value.IsPrimary,
            CreatedAt = value.CreatedAt,
            CreatedBy = value.CreatedBy,
            UpdatedAt = value.UpdatedAt,
            UpdatedBy = value.UpdatedBy,
            Version = value.Version,
            IsDeleted = value.IsDeleted,
            DeletedAt = value.DeletedAt,
            DeletedBy = value.DeletedBy
        };

        return this.ToActionResult(
            Result<GetArticlePrimaryMediaHttpResponse>.Success(response));
    }

    [HttpGet("attachments/state")]
    [Authorize(Policy = AuthorizationPolicies.MediaArticleMediaReadState)]
    [ProducesResponseType(typeof(GetArticleMediaSetHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStateAsync(
        [FromRoute] long articleId,
        CancellationToken cancellationToken)
    {
        var useCaseRequest = new GetArticleMediaSetRequest
        {
            ArticleId = articleId
        };

        Result<GetArticleMediaSetResponse> result =
            await _getArticleMediaSetUseCase.ExecuteAsync(
                useCaseRequest,
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetArticleMediaSetHttpResponse>.Failure(result.Error!));
        }

        GetArticleMediaSetResponse value = result.Value!;

        var response = new GetArticleMediaSetHttpResponse
        {
            ArticleId = value.ArticleId,
            Version = value.Version,
            CreatedAt = value.CreatedAt,
            CreatedBy = value.CreatedBy,
            UpdatedAt = value.UpdatedAt,
            UpdatedBy = value.UpdatedBy
        };

        return this.ToActionResult(
            Result<GetArticleMediaSetHttpResponse>.Success(response));
    }

    private static GetArticleMediaListItemHttpResponse MapListItem(
        GetArticleMediaListItemResponse item)
    {
        return new GetArticleMediaListItemHttpResponse
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
}