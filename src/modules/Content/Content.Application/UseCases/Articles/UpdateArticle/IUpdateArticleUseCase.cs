using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.UpdateArticle
{
    public interface IUpdateArticleUseCase
    {
        Task<Result<UpdateArticleResponseDto>> ExecuteAsync(
            UpdateArticleRequestDto request,
            CancellationToken cancellationToken = default);
    }
}