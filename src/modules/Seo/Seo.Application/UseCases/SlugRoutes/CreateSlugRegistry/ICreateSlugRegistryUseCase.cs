using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.CreateSlugRegistry;

public interface ICreateSlugRegistryUseCase
{
    Task<Result<CreateSlugRegistryResponse>> ExecuteAsync(
        CreateSlugRegistryRequest request,
        CancellationToken cancellationToken = default);
}