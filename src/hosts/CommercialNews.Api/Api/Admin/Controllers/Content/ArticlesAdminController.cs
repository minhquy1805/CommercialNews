using CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Requests;
using CommercialNews.Api.Api.Admin.Contracts.Content.Articles.Responses;
using CommercialNews.Api.Api.Common.Contracts;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.UseCases.ArticleLifecycleEvents.GetArticleLifecycleEvents;
using Content.Application.UseCases.ArticleRevisions.GetArticleRevisionById;
using Content.Application.UseCases.ArticleRevisions.GetArticleRevisions;
using Content.Application.UseCases.Articles.ArchiveArticle;
using Content.Application.UseCases.Articles.CreateArticle;
using Content.Application.UseCases.Articles.GetArticleById;
using Content.Application.UseCases.Articles.GetArticles;
using Content.Application.UseCases.Articles.PublishArticle;
using Content.Application.UseCases.Articles.SoftDeleteArticle;
using Content.Application.UseCases.Articles.UnpublishArticle;
using Content.Application.UseCases.Articles.UpdateArticle;
using Content.Application.UseCases.ArticleTags.GetArticleTags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Content
{
    [Authorize]
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
        private readonly ISoftDeleteArticleUseCase _softDeleteArticleUseCase;
        private readonly IRequestContext _requestContext;
        private readonly IGetArticleLifecycleEventsUseCase _getArticleLifecycleEventsUseCase;
        private readonly IGetArticleTagsUseCase _getArticleTagsUseCase;

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
            ISoftDeleteArticleUseCase softDeleteArticleUseCase,
            IRequestContext requestContext,
            IGetArticleLifecycleEventsUseCase getArticleLifecycleEventsUseCase,
            IGetArticleTagsUseCase getArticleTagsUseCase)
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
            _softDeleteArticleUseCase = softDeleteArticleUseCase;
            _requestContext = requestContext;
            _getArticleLifecycleEventsUseCase = getArticleLifecycleEventsUseCase;
            _getArticleTagsUseCase = getArticleTagsUseCase;
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesCreate)]
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
                CategoryId = request.CategoryId,
                AuthorUserId = request.AuthorUserId,
                Title = request.Title,
                Summary = request.Summary,
                Body = request.Body,
                CoverMediaId = request.CoverMediaId,
                TagIds = request.TagIds
            };

            Result<CreateArticleResponseDto> result =
                await _createArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(Result<CreateArticleResponse>.Failure(result.Error!));
            }

            var response = new CreateArticleResponse
            {
                ArticleId = result.Value.ArticleId,
                ArticlePublicId = result.Value.ArticlePublicId,
                CategoryId = result.Value.CategoryId,
                AuthorUserId = result.Value.AuthorUserId,
                Title = result.Value.Title,
                Summary = result.Value.Summary,
                Status = result.Value.Status,
                CoverMediaId = result.Value.CoverMediaId,
                TagIds = result.Value.TagIds,
                Version = result.Value.Version,
                CreatedAt = result.Value.CreatedAt
            };

            return CreatedAtRoute(
                GetArticleByIdRouteName,
                new { articleId = response.ArticleId },
                response);
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesRead)]
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
                ArticleId = result.Value.ArticleId,
                ArticlePublicId = result.Value.ArticlePublicId,
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

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesRead)]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ArticleListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPagedAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? status = null,
            [FromQuery] long? categoryId = null,
            [FromQuery] long? authorUserId = null,
            [FromQuery] bool isDeleted = false,
            [FromQuery] string? sort = "-updatedAt",
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new GetArticlesRequestDto
            {
                Page = page,
                PageSize = pageSize,
                Keyword = keyword,
                Status = status,
                CategoryId = categoryId,
                AuthorUserId = authorUserId,
                IsDeleted = isDeleted,
                Sort = sort
            };

            Result<PagedQueryResult<ArticleListItemDto>> result =
                await _getArticlesUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(
                    Result<PagedResponse<ArticleListItemResponse>>.Failure(result.Error!));
            }

            int totalPages = result.Value.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(result.Value.TotalItems / (double)result.Value.PageSize);

            var response = new PagedResponse<ArticleListItemResponse>
            {
                Items = result.Value.Items.Select(static item => new ArticleListItemResponse
                {
                    ArticleId = item.ArticleId,
                    ArticlePublicId = item.ArticlePublicId,
                    Title = item.Title,
                    Summary = item.Summary,
                    Status = item.Status,
                    AuthorUserId = item.AuthorUserId,
                    CategoryId = item.CategoryId,
                    CoverMediaId = item.CoverMediaId,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    PublishedAt = item.PublishedAt,
                    UnpublishedAt = item.UnpublishedAt,
                    ArchivedAt = item.ArchivedAt,
                    IsDeleted = item.IsDeleted,
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

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesUpdate)]
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
                CategoryId = request.CategoryId,
                Title = request.Title,
                Summary = request.Summary,
                Body = request.Body,
                CoverMediaId = request.CoverMediaId,
                TagIds = request.TagIds,
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
                ArticleId = result.Value.ArticleId,
                ArticlePublicId = result.Value.ArticlePublicId,
                CategoryId = result.Value.CategoryId,
                AuthorUserId = result.Value.AuthorUserId,
                Title = result.Value.Title,
                Summary = result.Value.Summary,
                Body = result.Value.Body,
                Status = result.Value.Status,
                CoverMediaId = result.Value.CoverMediaId,
                TagIds = result.Value.TagIds,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(Result<UpdateArticleResponse>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesReadRevisions)]
        [HttpGet("{articleId:long}/revisions")]
        [ProducesResponseType(typeof(IReadOnlyList<ArticleRevisionItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRevisionsAsync(
            [FromRoute] long articleId,
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new GetArticleRevisionsRequestDto
            {
                ArticleId = articleId
            };

            Result<IReadOnlyList<ArticleRevisionItemDto>> result =
                await _getArticleRevisionsUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(
                    Result<IReadOnlyList<ArticleRevisionItemResponse>>.Failure(result.Error!));
            }

            IReadOnlyList<ArticleRevisionItemResponse> response = result.Value
                .Select(static item => new ArticleRevisionItemResponse
                {
                    RevisionId = item.RevisionId,
                    ArticleId = item.ArticleId,
                    EditedAt = item.EditedAt,
                    EditedByUserId = item.EditedByUserId,
                    ArticleVersion = item.ArticleVersion,
                    CorrelationId = item.CorrelationId,
                    ChangeSummary = item.ChangeSummary,
                    OldTitle = item.OldTitle,
                    OldSummary = item.OldSummary,
                    OldBody = item.OldBody
                })
                .ToArray();

            return this.ToActionResult(
                Result<IReadOnlyList<ArticleRevisionItemResponse>>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesReadRevisions)]
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
                return this.ToActionResult(
                    Result<GetArticleRevisionByIdResponse>.Failure(result.Error!));
            }

            var response = new GetArticleRevisionByIdResponse
            {
                RevisionId = result.Value.RevisionId,
                ArticleId = result.Value.ArticleId,
                EditedAt = result.Value.EditedAt,
                EditedByUserId = result.Value.EditedByUserId,
                ArticleVersion = result.Value.ArticleVersion,
                CorrelationId = result.Value.CorrelationId,
                ChangeSummary = result.Value.ChangeSummary,
                OldTitle = result.Value.OldTitle,
                OldSummary = result.Value.OldSummary,
                OldBody = result.Value.OldBody
            };

            return this.ToActionResult(
                Result<GetArticleRevisionByIdResponse>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesPublish)]
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
                return this.ToActionResult(
                    Result<PublishArticleResponse>.Failure(result.Error!));
            }

            var response = new PublishArticleResponse
            {
                ArticleId = result.Value.ArticleId,
                ArticlePublicId = result.Value.ArticlePublicId,
                Status = result.Value.Status,
                PublishedAt = result.Value.PublishedAt,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(
                Result<PublishArticleResponse>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesUnpublish)]
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
                return this.ToActionResult(
                    Result<UnpublishArticleResponse>.Failure(result.Error!));
            }

            var response = new UnpublishArticleResponse
            {
                ArticleId = result.Value.ArticleId,
                ArticlePublicId = result.Value.ArticlePublicId,
                Status = result.Value.Status,
                UnpublishedAt = result.Value.UnpublishedAt,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(
                Result<UnpublishArticleResponse>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesArchive)]
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
                return this.ToActionResult(
                    Result<ArchiveArticleResponse>.Failure(result.Error!));
            }

            var response = new ArchiveArticleResponse
            {
                ArticleId = result.Value.ArticleId,
                ArticlePublicId = result.Value.ArticlePublicId,
                Status = result.Value.Status,
                ArchivedAt = result.Value.ArchivedAt,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt
            };

            return this.ToActionResult(
                Result<ArchiveArticleResponse>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesDelete)]
        [HttpDelete("{articleId:long}")]
        [ProducesResponseType(typeof(SoftDeleteArticleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SoftDeleteAsync(
            [FromRoute] long articleId,
            [FromBody] SoftDeleteArticleRequest request,
            CancellationToken cancellationToken)
        {
            var useCaseRequest = new SoftDeleteArticleRequestDto
            {
                ArticleId = articleId,
                ExpectedVersion = request.ExpectedVersion,
                ActorUserId = _requestContext.CurrentUserId
            };

            Result<SoftDeleteArticleResponseDto> result =
                await _softDeleteArticleUseCase.ExecuteAsync(useCaseRequest, cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(
                    Result<SoftDeleteArticleResponse>.Failure(result.Error!));
            }

            var response = new SoftDeleteArticleResponse
            {
                ArticleId = result.Value.ArticleId,
                ArticlePublicId = result.Value.ArticlePublicId,
                IsDeleted = result.Value.IsDeleted,
                Version = result.Value.Version,
                UpdatedAt = result.Value.UpdatedAt,
                DeletedAt = result.Value.DeletedAt,
                DeletedByUserId = result.Value.DeletedByUserId
            };

            return this.ToActionResult(
                Result<SoftDeleteArticleResponse>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesReadLifecycleEvents)]
        [HttpGet("{articleId:long}/lifecycle-events")]
        [ProducesResponseType(typeof(IReadOnlyList<ArticleLifecycleEventItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLifecycleEventsAsync(
            [FromRoute] long articleId,
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new GetArticleLifecycleEventsRequestDto
            {
                ArticleId = articleId
            };

            Result<IReadOnlyList<ArticleLifecycleEventItemDto>> result =
                await _getArticleLifecycleEventsUseCase.ExecuteAsync(
                    useCaseRequest,
                    cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(
                    Result<IReadOnlyList<ArticleLifecycleEventItemResponse>>.Failure(result.Error!));
            }

            IReadOnlyList<ArticleLifecycleEventItemResponse> response = result.Value
                .Select(static item => new ArticleLifecycleEventItemResponse
                {
                    EventId = item.EventId,
                    ArticleId = item.ArticleId,
                    ArticleVersion = item.ArticleVersion,
                    ActionType = item.ActionType,
                    FromStatus = item.FromStatus,
                    ToStatus = item.ToStatus,
                    Reason = item.Reason,
                    ActorUserId = item.ActorUserId,
                    OccurredAt = item.OccurredAt,
                    CorrelationId = item.CorrelationId,
                    MetadataJson = item.MetadataJson
                })
                .ToArray();

            return this.ToActionResult(
                Result<IReadOnlyList<ArticleLifecycleEventItemResponse>>.Success(response));
        }

        [Authorize(Policy = AuthorizationPolicies.ContentArticlesReadTags)]
        [HttpGet("{articleId:long}/tags")]
        [ProducesResponseType(typeof(IReadOnlyList<ArticleTagItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTagsAsync(
            [FromRoute] long articleId,
            CancellationToken cancellationToken = default)
        {
            var useCaseRequest = new GetArticleTagsRequestDto
            {
                ArticleId = articleId
            };

            Result<IReadOnlyList<ArticleTagItemDto>> result =
                await _getArticleTagsUseCase.ExecuteAsync(
                    useCaseRequest,
                    cancellationToken);

            if (result.IsFailure)
            {
                return this.ToActionResult(
                    Result<IReadOnlyList<ArticleTagItemResponse>>.Failure(result.Error!));
            }

            IReadOnlyList<ArticleTagItemResponse> response = result.Value
                .Select(static item => new ArticleTagItemResponse
                {
                    ArticleId = item.ArticleId,
                    TagId = item.TagId,
                    AttachedAt = item.AttachedAt,
                    AttachedByUserId = item.AttachedByUserId
                })
                .ToArray();

            return this.ToActionResult(
                Result<IReadOnlyList<ArticleTagItemResponse>>.Success(response));
        }
    }
}