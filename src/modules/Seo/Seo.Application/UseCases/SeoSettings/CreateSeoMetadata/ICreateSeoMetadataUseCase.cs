using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.CreateSeoMetadata;

public interface ICreateSeoMetadataUseCase
{
    Task<Result<CreateSeoMetadataResponse>> ExecuteAsync(
        CreateSeoMetadataRequest request,
        CancellationToken cancellationToken = default);
}