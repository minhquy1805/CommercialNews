using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataByResource;

public interface IGetSeoMetadataByResourceUseCase
{
    Task<Result<GetSeoMetadataByResourceResponse>> ExecuteAsync(
        GetSeoMetadataByResourceRequest request,
        CancellationToken cancellationToken = default);
}