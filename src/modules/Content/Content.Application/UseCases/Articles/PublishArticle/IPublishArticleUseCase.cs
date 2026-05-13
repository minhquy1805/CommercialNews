using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;

namespace Content.Application.UseCases.Articles.PublishArticle;

public interface IPublishArticleUseCase
{
    Task<Result<PublishArticleResponseDto>> ExecuteAsync(
        PublishArticleRequestDto request,
        CancellationToken cancellationToken = default);
}