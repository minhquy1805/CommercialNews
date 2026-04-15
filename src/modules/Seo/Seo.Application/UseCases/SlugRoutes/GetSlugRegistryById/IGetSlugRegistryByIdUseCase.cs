using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryById;

public interface IGetSlugRegistryByIdUseCase
{
    Task<Result<GetSlugRegistryByIdResponse>> ExecuteAsync(
        GetSlugRegistryByIdRequest request,
        CancellationToken cancellationToken = default);
}