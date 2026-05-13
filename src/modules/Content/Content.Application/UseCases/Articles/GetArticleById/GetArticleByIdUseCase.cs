using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;

namespace Content.Application.UseCases.Articles.GetArticleById;

public sealed class GetArticleByIdUseCase : IGetArticleByIdUseCase
{
    private readonly IArticleRepository _articleRepository;

    public GetArticleByIdUseCase(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
    }

    public async Task<Result<GetArticleByIdResponseDto>> ExecuteAsync(
        GetArticleByIdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<GetArticleByIdResponseDto>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        Article? article = await _articleRepository.GetByIdAsync(
            request.ArticleId,
            cancellationToken);

        if (article is null)
        {
            return Result<GetArticleByIdResponseDto>.Failure(
                ContentErrors.Article.NotFound);
        }

        return Result<GetArticleByIdResponseDto>.Success(
            new GetArticleByIdResponseDto
            {
                ArticleId = article.ArticleId,
                ArticlePublicId = article.ArticlePublicId,
                CategoryId = article.CategoryId,
                AuthorUserId = article.AuthorUserId,
                Title = article.Title,
                Summary = article.Summary,
                Body = article.Body,
                Status = article.Status,
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
