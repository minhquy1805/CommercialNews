using CommercialNews.BuildingBlocks.SharedKernel.Paging;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;

public interface IGetSlugRegistryListUseCase
{
    Task<Result<PagedQueryResult<GetSlugRegistryListResponse>>> ExecuteAsync(
        GetSlugRegistryListRequest request,
        CancellationToken cancellationToken = default);
}