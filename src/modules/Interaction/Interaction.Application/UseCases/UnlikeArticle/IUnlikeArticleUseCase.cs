using CommercialNews.BuildingBlocks.Results;
using Interaction.Application.Contracts.Likes.Requests;
using Interaction.Application.Contracts.Likes.Responses;

namespace Interaction.Application.UseCases.UnlikeArticle;

public interface IUnlikeArticleUseCase
{
    Task<Result<UnlikeArticleResponse>> ExecuteAsync(
        UnlikeArticleRequest request,
        CancellationToken cancellationToken = default);
}