using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.UpdateSeoMetadata;

public interface IUpdateSeoMetadataUseCase
{
    Task<Result<UpdateSeoMetadataResponse>> ExecuteAsync(
        UpdateSeoMetadataRequest request,
        CancellationToken cancellationToken = default);
}