using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.ActivateSlugRegistry;

public interface IActivateSlugRegistryUseCase
{
    Task<Result<ActivateSlugRegistryResponse>> ExecuteAsync(
        ActivateSlugRegistryRequest request,
        CancellationToken cancellationToken = default);
}