using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.DeactivateSlugRegistry;

public interface IDeactivateSlugRegistryUseCase
{
    Task<Result<DeactivateSlugRegistryResponse>> ExecuteAsync(
        DeactivateSlugRegistryRequest request,
        CancellationToken cancellationToken = default);
}