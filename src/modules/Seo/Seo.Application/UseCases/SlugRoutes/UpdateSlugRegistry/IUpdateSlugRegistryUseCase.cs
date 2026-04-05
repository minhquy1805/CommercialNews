using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.UpdateSlugRegistry;

public interface IUpdateSlugRegistryUseCase
{
    Task<Result<UpdateSlugRegistryResponse>> ExecuteAsync(
        UpdateSlugRegistryRequest request,
        CancellationToken cancellationToken = default);
}