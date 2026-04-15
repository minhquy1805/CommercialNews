using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Media.Application.Contracts.ArticleMedia.Requests;
using Media.Application.Contracts.ArticleMedia.Responses;

namespace Media.Application.UseCases.ArticleMedia.SetPrimaryMedia;

public interface ISetPrimaryMediaUseCase
{
    Task<Result<SetPrimaryMediaResponse>> ExecuteAsync(
        SetPrimaryMediaRequest request,
        CancellationToken cancellationToken = default);
}