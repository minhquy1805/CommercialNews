using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Articles.Requests;
using Reading.Application.Contracts.Articles.Responses;
using Reading.Application.Models.Queries;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Articles;

namespace Reading.Application.UseCases.Articles.GetRelatedArticles;

public sealed class GetRelatedArticlesUseCase : IGetRelatedArticlesUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;

    public GetRelatedArticlesUseCase(
        IArticleReadModelRepository articleReadModelRepository)
    {
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));
    }

    public async Task<Result<IReadOnlyList<ArticleListItemResponse>>> ExecuteAsync(
        GetRelatedArticlesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? validationError =
            GetRelatedArticlesValidator.Validate(request);

        if (validationError is not null)
        {
            return Result<IReadOnlyList<ArticleListItemResponse>>.Failure(
                validationError);
        }

        string articlePublicId =
            GetRelatedArticlesValidator.NormalizeArticlePublicId(
                request.ArticlePublicId);

        var query = new GetRelatedArticlesQuery(
            ArticlePublicId: articlePublicId,
            Limit: request.Limit);

        IReadOnlyList<ArticleListItemResult> relatedArticles =
            await _articleReadModelRepository.SelectRelatedAsync(
                query,
                cancellationToken);

        IReadOnlyList<ArticleListItemResponse> response =
            relatedArticles
                .Select(MapToResponse)
                .ToList();

        return Result<IReadOnlyList<ArticleListItemResponse>>.Success(
            response);
    }

    private static ArticleListItemResponse MapToResponse(
        ArticleListItemResult item)
    {
        return new ArticleListItemResponse
        {
            ArticlePublicId = item.ArticlePublicId,
            Slug = item.Slug,

            Title = item.Title,
            Summary = item.Summary,

            CategoryId = item.CategoryId,
            CategoryName = item.CategoryName,

            AuthorUserId = item.AuthorUserId,
            AuthorDisplayName = item.AuthorDisplayName,

            CoverMediaId = item.CoverMediaId,
            CoverMediaUrl = item.CoverMediaUrl,
            CoverAlt = item.CoverAlt,

            PublishedAtUtc = item.PublishedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,

            ViewCount = item.ViewCount,
            LikeCount = item.LikeCount,
            VisibleCommentCount = item.VisibleCommentCount,
            CountersPartial = item.CountersPartial
        };
    }
}
