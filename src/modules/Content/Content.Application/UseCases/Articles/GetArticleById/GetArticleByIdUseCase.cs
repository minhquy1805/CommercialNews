using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Articles.GetArticleById
{
    public sealed class GetArticleByIdUseCase : IGetArticleByIdUseCase
    {
        private readonly IArticleRepository _articleRepository;

        public GetArticleByIdUseCase(IArticleRepository articleRepository)
        {
            _articleRepository = articleRepository;
        }

        public async Task<Result<GetArticleByIdResponseDto>> ExecuteAsync(
            GetArticleByIdRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.ArticleId <= 0)
            {
                return Result<GetArticleByIdResponseDto>.Failure(ContentErrors.Article.InvalidArticleId);
            }

            var article = await _articleRepository.GetByIdAsync(request.ArticleId, cancellationToken);

            if (article is null)
            {
                return Result<GetArticleByIdResponseDto>.Failure(ContentErrors.Article.NotFound);
            }

            return Result<GetArticleByIdResponseDto>.Success(new GetArticleByIdResponseDto
            {
                ArticleId = article.ArticleId,
                PublicId = article.PublicId,
                Title = article.Title,
                Summary = article.Summary,
                Body = article.Body,
                Status = article.Status,
                AuthorUserId = article.AuthorUserId,
                CategoryId = article.CategoryId,
                CoverMediaId = article.CoverMediaId,
                CreatedAt = article.CreatedAt,
                UpdatedAt = article.UpdatedAt,
                PublishedAt = article.PublishedAt,
                UnpublishedAt = article.UnpublishedAt,
                ArchivedAt = article.ArchivedAt,
                CreatedByUserId = article.CreatedByUserId,
                UpdatedByUserId = article.UpdatedByUserId,
                IsDeleted = article.IsDeleted,
                DeletedAt = article.DeletedAt,
                DeletedByUserId = article.DeletedByUserId,
                Version = article.Version
            });
        }
    }    
}

