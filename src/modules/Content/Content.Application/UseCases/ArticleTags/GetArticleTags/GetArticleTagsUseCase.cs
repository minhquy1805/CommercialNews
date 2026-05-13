using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;

namespace Content.Application.UseCases.ArticleTags.GetArticleTags;

public sealed class GetArticleTagsUseCase : IGetArticleTagsUseCase
{
    private readonly IArticleTagRepository _articleTagRepository;

    public GetArticleTagsUseCase(IArticleTagRepository articleTagRepository)
    {
        _articleTagRepository = articleTagRepository
            ?? throw new ArgumentNullException(nameof(articleTagRepository));
    }

    public async Task<Result<IReadOnlyList<ArticleTagItemDto>>> ExecuteAsync(
        GetArticleTagsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<IReadOnlyList<ArticleTagItemDto>>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        IReadOnlyList<ArticleTag> articleTags =
            await _articleTagRepository.GetByArticleIdAsync(
                request.ArticleId,
                cancellationToken);

        IReadOnlyList<ArticleTagItemDto> response = articleTags
            .Select(static articleTag => new ArticleTagItemDto
            {
                ArticleId = articleTag.ArticleId,
                TagId = articleTag.TagId,
                AttachedAt = articleTag.AttachedAt,
                AttachedByUserId = articleTag.AttachedByUserId
            })
            .ToArray();

        return Result<IReadOnlyList<ArticleTagItemDto>>.Success(response);
    }
}