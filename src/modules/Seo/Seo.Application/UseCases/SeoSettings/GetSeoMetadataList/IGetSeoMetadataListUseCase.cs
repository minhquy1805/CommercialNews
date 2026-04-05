using CommercialNews.BuildingBlocks.Contracts.Common;
using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataList;

public interface IGetSeoMetadataListUseCase
{
    Task<Result<PagedQueryResult<GetSeoMetadataListResponse>>> ExecuteAsync(
        GetSeoMetadataListRequest request,
        CancellationToken cancellationToken = default);
}