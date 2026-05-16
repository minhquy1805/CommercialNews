using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryByResource;

public interface IGetSlugRegistryByResourceUseCase
{
    Task<Result<GetSlugRegistryByResourceResponse>> ExecuteAsync(
        GetSlugRegistryByResourceRequest request,
        CancellationToken cancellationToken = default);
}