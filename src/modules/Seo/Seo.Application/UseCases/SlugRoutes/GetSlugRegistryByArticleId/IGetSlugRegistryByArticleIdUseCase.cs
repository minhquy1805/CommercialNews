using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByArticleId;

public interface IGetSlugRegistryByArticleIdUseCase
{
    Task<Result<IReadOnlyList<GetSlugRegistryByArticleIdResponse>>> ExecuteAsync(
        GetSlugRegistryByArticleIdRequest request,
        CancellationToken cancellationToken = default);
}