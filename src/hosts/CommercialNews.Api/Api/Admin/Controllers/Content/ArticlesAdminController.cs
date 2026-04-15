using CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses;
using CommercialNews.Api.Api.Common.Contracts;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.UseCases.Articles.ArchiveArticle;
using Content.Application.UseCases.Articles.CreateArticle;
using Content.Application.UseCases.Articles.DeleteArticle;
using Content.Application.UseCases.Articles.GetArticleById;
using Content.Application.UseCases.Articles.GetArticleRevisionById;
using Content.Application.UseCases.Articles.GetArticleRevisions;
using Content.Application.UseCases.Articles.GetArticles;
using Content.Application.UseCases.Articles.PublishArticle;
using Content.Application.UseCases.Articles.RestoreArticle;
using Content.Application.UseCases.Articles.UnpublishArticle;
using Content.Application.UseCases.Articles.UpdateArticle;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Content
{
    [ApiController]
    [Route("api/v1/admin/content/articles")]
    public sealed class ArticlesAdminController : ControllerBase
    {
        private const string GetArticleByIdRouteName = "AdminContentArticles.GetById";

        private readonly ICreateArticleUseCase _createArticleUseCase;
        private readonly IGetArticleByIdUseCase _getArticleByIdUseCase;
        private readonly IGetArticlesUseCase _getArticlesUseCase;
        private readonly IUpdateArticleUseCase _updateArticleUseCase;
        private readonly IGetArticleRevisionsUseCase _getArticleRevisionsUseCase;
        private readonly IGetArticleRevisionByIdUseCase _getArticleRevisionByIdUseCase;
        private readonly IPublishArticleUseCase _publishArticleUseCase;
        private readonly IUnpublishArticleUseCase _unpublishArticleUseCase;
        private readonly IArchiveArticleUseCase _archiveArticleUseCase;
        private readonly IRestoreArticleUseCase _restoreArticleUseCase;
        private readonly IDeleteArticleUseCase _deleteArticleUseCase;

        public ArticlesAdminController(
            ICreateArticleUseCase createArticleUseCase,
            IGetArticleByIdUseCase getArticleByIdUseCase,
            IGetArticlesUseCase getArticlesUseCase,
            IUpdateArticleUseCase updateArticleUseCase,
            IGetArticleRevisionsUseCase getArticleRevisionsUseCase,
            IGetArticleRevisionByIdUseCase getArticleRevisionByIdUseCase,
            IPublishArticleUseCase publishArticleUseCase,
            IUnpublishArticleUseCase unpublishArticleUseCase,
            IArchiveArticleUseCase archiveArticleUseCase,
            IRestoreArticleUseCase restoreArticleUseCase,
            IDeleteArticleUseCase deleteArticleUseCase)
        {
            _createArticleUseCase = createArticleUseCase;
            _getArticleByIdUseCase = getArticleByIdUseCase;
            _getArticlesUseCase = getArticlesUseCase;
            _updateArticleUseCase = updateArticleUseCase;
            _getArticleRevisionsUseCase = getArticleRevisionsUseCase;
            _getArticleRevisionByIdUseCase = getArticleRevisionByIdUseCase;
            _publishArticleUseCase = publishArticleUseCase;
            _unpublishArticleUseCase = unpublishArticleUseCase;
            _archiveArticleUseCase = archiveArticleUseCase;
            _restoreArticleUseCase = restoreArticleUseCase;
            _deleteArticleUseCase = deleteArticleUseCase;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateArticleResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new CreateArticleRequestDto
            {
                Title = request.Title,
                Summary = request.Summary,
                Body = request.Body,
                AuthorUserId = request.AuthorUserId,
                CategoryId = request.CategoryId,
                CoverMediaId = request.CoverMediaId
            };

            Result<CreateArticleResponseDto> result =
                await _createArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<CreateArticleResponse>.Failure(result.Error!));
            }

            var response = new CreateArticleResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                Status = result.Value.Status,
                Version = result.Value.Version,
                CreatedAt = result.Value.CreatedAt
            };

            return CreatedAtRoute(
                GetArticleByIdRouteName,
                new { articleId = response.ArticleId },
                response);
        }

        [HttpGet("{articleId:long}", Name = GetArticleByIdRouteName)]
        [ProducesResponseType(typeof(GetArticleByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(
            [FromRoute] long articleId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new GetArticleByIdRequestDto
            {
                ArticleId = articleId
            };

            Result<GetArticleByIdResponseDto> result =
                await _getArticleByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetArticleByIdResponse>.Failure(result.Error!));
            }

            var response = new GetArticleByIdResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                Title = result.Value.Title,
                Summary = result.Value.Summary,
                Body = result.Value.Body,
                Status = result.Value.Status,
                AuthorUserId = result.Value.AuthorUserId,
                CategoryId = result.Value.CategoryId,
                CoverMediaId = result.Value.CoverMediaId,
                CreatedAt = result.Value.CreatedAt,
                UpdatedAt = result.Value.UpdatedAt,
                PublishedAt = result.Value.PublishedAt,
                UnpublishedAt = result.Value.UnpublishedAt,
                ArchivedAt = result.Value.ArchivedAt,
                CreatedByUserId = result.Value.CreatedByUserId,
                UpdatedByUserId = result.Value.UpdatedByUserId,
                IsDeleted = result.Value.IsDeleted,
                DeletedAt = result.Value.DeletedAt,
                DeletedByUserId = result.Value.DeletedByUserId,
                Version = result.Value.Version
            };

            return this.ToActionResult(Result<GetArticleByIdResponse>.Success(response));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ArticleListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] long? categoryId = null,
            [FromQuery] long? tagId = null,
            [FromQuery] string? sort = "-updatedAt",
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new GetArticlesRequestDto
            {
                Page = page,
                PageSize = pageSize,
                Status = status,
                CategoryId = categoryId,
                TagId = tagId,
                Sort = sort
            };

            Result<PagedQueryResult<ArticleListItemDto>> result =
                await _getArticlesUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(
                    Result<PagedResponse<ArticleListItemResponse>>.Failure(result.Error!));
            }

            int totalPages = result.Value!.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(result.Value.TotalItems / (double)result.Value.PageSize);

            var response = new PagedResponse<ArticleListItemResponse>
            {
                Items = result.Value.Items.Select(static item => new ArticleListItemResponse
                {
                    ArticleId = item.ArticleId,
                    PublicId = item.PublicId,
                    Title = item.Title,
                    Summary = item.Summary,
                    Status = item.Status,
                    AuthorUserId = item.AuthorUserId,
                    CategoryId = item.CategoryId,
                    CoverMediaId = item.CoverMediaId,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    PublishedAt = item.PublishedAt,
                    Version = item.Version
                }).ToArray(),
                PageInfo = new PageInfo
                {
                    Page = result.Value.Page,
                    PageSize = result.Value.PageSize,
                    TotalItems = result.Value.TotalItems,
                    TotalPages = totalPages
                }
            };

            return this.ToActionResult(
                Result<PagedResponse<ArticleListItemResponse>>.Success(response));
        }

        [HttpPut("{articleId:long}")]
        [ProducesResponseType(typeof(UpdateArticleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateAsync(
            [FromRoute] long articleId,
            [FromBody] UpdateArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new UpdateArticleRequestDto
            {
                ArticleId = articleId,
                ExpectedVersion = request.ExpectedVersion,
                Title = request.Title,
                Summary = request.Summary,
                Body = request.Body,
                CategoryId = request.CategoryId,
                CoverMediaId = request.CoverMediaId,
                ChangeSummary = request.ChangeSummary
            };

            Result<UpdateArticleResponseDto> result =
                await _updateArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<UpdateArticleResponse>.Failure(result.Error!));
            }

            var response = new UpdateArticleResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                Title = result.Value.Title,
                Summary = result.Value.Summary,
                Body = result.Value.Body,
                Status = result.Value.Status,
                CategoryId = result.Value.CategoryId,
                CoverMediaId = result.Value.CoverMediaId,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<UpdateArticleResponse>.Success(response));
        }

        [HttpGet("{articleId:long}/revisions")]
        [ProducesResponseType(typeof(PagedResponse<ArticleRevisionItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRevisionsAsync(
            [FromRoute] long articleId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new GetArticleRevisionsRequestDto
            {
                ArticleId = articleId,
                Page = page,
                PageSize = pageSize
            };

            Result<PagedQueryResult<ArticleRevisionListItemDto>> result =
                await _getArticleRevisionsUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(
                    Result<PagedResponse<ArticleRevisionItemResponse>>.Failure(result.Error!));
            }

            int totalPages = result.Value!.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(result.Value.TotalItems / (double)result.Value.PageSize);

            var response = new PagedResponse<ArticleRevisionItemResponse>
            {
                Items = result.Value.Items.Select(static item => new ArticleRevisionItemResponse
                {
                    RevisionId = item.RevisionId,
                    RevisionNumber = item.RevisionNumber,
                    TitleSnapshot = item.TitleSnapshot,
                    SummarySnapshot = item.SummarySnapshot,
                    BodySnapshot = item.BodySnapshot,
                    CategoryIdSnapshot = item.CategoryIdSnapshot,
                    StatusSnapshot = item.StatusSnapshot,
                    CoverMediaIdSnapshot = item.CoverMediaIdSnapshot,
                    ChangedAt = item.ChangedAt,
                    ChangedByUserId = item.ChangedByUserId,
                    ChangeType = item.ChangeType,
                    ChangeSummary = item.ChangeSummary
                }).ToArray(),
                PageInfo = new PageInfo
                {
                    Page = result.Value.Page,
                    PageSize = result.Value.PageSize,
                    TotalItems = result.Value.TotalItems,
                    TotalPages = totalPages
                }
            };

            return this.ToActionResult(Result<PagedResponse<ArticleRevisionItemResponse>>.Success(response));
        }

        [HttpGet("{articleId:long}/revisions/{revisionId:long}")]
        [ProducesResponseType(typeof(GetArticleRevisionByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRevisionByIdAsync(
            [FromRoute] long articleId,
            [FromRoute] long revisionId,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new GetArticleRevisionByIdRequestDto
            {
                ArticleId = articleId,
                RevisionId = revisionId
            };

            Result<GetArticleRevisionByIdResponseDto> result =
                await _getArticleRevisionByIdUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<GetArticleRevisionByIdResponse>.Failure(result.Error!));
            }

            var response = new GetArticleRevisionByIdResponse
            {
                RevisionId = result.Value!.RevisionId,
                ArticleId = result.Value.ArticleId,
                RevisionNumber = result.Value.RevisionNumber,
                TitleSnapshot = result.Value.TitleSnapshot,
                SummarySnapshot = result.Value.SummarySnapshot,
                BodySnapshot = result.Value.BodySnapshot,
                CategoryIdSnapshot = result.Value.CategoryIdSnapshot,
                StatusSnapshot = result.Value.StatusSnapshot,
                CoverMediaIdSnapshot = result.Value.CoverMediaIdSnapshot,
                ChangedAt = result.Value.ChangedAt,
                ChangedByUserId = result.Value.ChangedByUserId,
                ChangeType = result.Value.ChangeType,
                ChangeSummary = result.Value.ChangeSummary
            };

            return this.ToActionResult(Result<GetArticleRevisionByIdResponse>.Success(response));
        }

        [HttpPost("{articleId:long}:publish")]
        [ProducesResponseType(typeof(PublishArticleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PublishAsync(
            [FromRoute] long articleId,
            [FromBody] PublishArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new PublishArticleRequestDto
            {
                ArticleId = articleId,
                ExpectedVersion = request.ExpectedVersion
            };

            Result<PublishArticleResponseDto> result =
                await _publishArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<PublishArticleResponse>.Failure(result.Error!));
            }

            var response = new PublishArticleResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                Status = result.Value.Status,
                PublishedAt = result.Value.PublishedAt,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<PublishArticleResponse>.Success(response));
        }

        [HttpPost("{articleId:long}:unpublish")]
        [ProducesResponseType(typeof(UnpublishArticleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UnpublishAsync(
            [FromRoute] long articleId,
            [FromBody] UnpublishArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new UnpublishArticleRequestDto
            {
                ArticleId = articleId,
                ExpectedVersion = request.ExpectedVersion,
                Reason = request.Reason
            };

            Result<UnpublishArticleResponseDto> result =
                await _unpublishArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<UnpublishArticleResponse>.Failure(result.Error!));
            }

            var response = new UnpublishArticleResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                Status = result.Value.Status,
                UnpublishedAt = result.Value.UnpublishedAt,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<UnpublishArticleResponse>.Success(response));
        }

        [HttpPost("{articleId:long}:archive")]
        [ProducesResponseType(typeof(ArchiveArticleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ArchiveAsync(
            [FromRoute] long articleId,
            [FromBody] ArchiveArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new ArchiveArticleRequestDto
            {
                ArticleId = articleId,
                ExpectedVersion = request.ExpectedVersion
            };

            Result<ArchiveArticleResponseDto> result =
                await _archiveArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<ArchiveArticleResponse>.Failure(result.Error!));
            }

            var response = new ArchiveArticleResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                Status = result.Value.Status,
                ArchivedAt = result.Value.ArchivedAt,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<ArchiveArticleResponse>.Success(response));
        }

        [HttpPost("{articleId:long}:restore")]
        [ProducesResponseType(typeof(RestoreArticleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RestoreAsync(
            [FromRoute] long articleId,
            [FromBody] RestoreArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new RestoreArticleRequestDto
            {
                ArticleId = articleId,
                ExpectedVersion = request.ExpectedVersion
            };

            Result<RestoreArticleResponseDto> result =
                await _restoreArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<RestoreArticleResponse>.Failure(result.Error!));
            }

            var response = new RestoreArticleResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                Status = result.Value.Status,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<RestoreArticleResponse>.Success(response));
        }

        [HttpDelete("{articleId:long}")]
        [ProducesResponseType(typeof(DeleteArticleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute] long articleId,
            [FromBody] DeleteArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new DeleteArticleRequestDto
            {
                ArticleId = articleId,
                ExpectedVersion = request.ExpectedVersion
            };

            Result<DeleteArticleResponseDto> result =
                await _deleteArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<DeleteArticleResponse>.Failure(result.Error!));
            }

            var response = new DeleteArticleResponse
            {
                ArticleId = result.Value!.ArticleId,
                PublicId = result.Value.PublicId,
                IsDeleted = result.Value.IsDeleted,
                DeletedAt = result.Value.DeletedAt,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<DeleteArticleResponse>.Success(response));
        }
    }
}