using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.GetSlugRegistryList;

public interface IGetSlugRegistryListUseCase
{
    Task<Result<PagedQueryResult<GetSlugRegistryListResponse>>> ExecuteAsync(
        GetSlugRegistryListRequest request,
        CancellationToken cancellationToken = default);
}