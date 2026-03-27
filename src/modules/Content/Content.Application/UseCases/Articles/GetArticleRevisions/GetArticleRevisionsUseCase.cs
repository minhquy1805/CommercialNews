using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Articles.GetArticleRevisions
{
    public sealed class GetArticleRevisionsUseCase : IGetArticleRevisionsUseCase
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IArticleRevisionRepository _articleRevisionRepository;

        public GetArticleRevisionsUseCase(
            IArticleRepository articleRepository,
            IArticleRevisionRepository articleRevisionRepository)
        {
            _articleRepository = articleRepository;
            _articleRevisionRepository = articleRevisionRepository;
        }

        public async Task<Result<PagedResponse<ArticleRevisionListItemDto>>> ExecuteAsync(
            GetArticleRevisionsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.ArticleId <= 0)
            {
                return Result<PagedResponse<ArticleRevisionListItemDto>>.Failure(
                    Content.Application.Errors.ContentErrors.Article.InvalidArticleId);
            }

            if (request.Page <= 0)
            {
                return Result<PagedResponse<ArticleRevisionListItemDto>>.Failure(
                    Error.Validation(
                        code: "CONTENT.INVALID_PAGE",
                        message: "Page must be greater than zero."));
            }

            if (request.PageSize <= 0)
            {
                return Result<PagedResponse<ArticleRevisionListItemDto>>.Failure(
                    Error.Validation(
                        code: "CONTENT.INVALID_PAGE_SIZE",
                        message: "PageSize must be greater than zero."));
            }

            if (request.PageSize > 100)
            {
                return Result<PagedResponse<ArticleRevisionListItemDto>>.Failure(
                    Error.Validation(
                        code: "CONTENT.INVALID_PAGE_SIZE",
                        message: "PageSize must not exceed 100."));
            }

            var article = await _articleRepository.GetByIdAsync(
                request.ArticleId,
                cancellationToken);

            if (article is null)
            {
                return Result<PagedResponse<ArticleRevisionListItemDto>>.Failure(
                    Content.Application.Errors.ContentErrors.Article.NotFound);
            }

            var query = new ArticleRevisionListQuery
            {
                ArticleId = request.ArticleId,
                Page = request.Page,
                PageSize = request.PageSize
            };

            PagedQueryResult<ArticleRevisionListResultItem> result =
                await _articleRevisionRepository.GetPagedByArticleIdAsync(query, cancellationToken);

            int totalPages = result.TotalItems == 0
                ? 0
                : (int)Math.Ceiling(result.TotalItems / (double)result.PageSize);

            var response = new PagedResponse<ArticleRevisionListItemDto>
            {
                Items = result.Items.Select(static item => new ArticleRevisionListItemDto
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
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalItems = result.TotalItems,
                    TotalPages = totalPages
                }
            };

            return Result<PagedResponse<ArticleRevisionListItemDto>>.Success(response);
        }
    }
}