using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataById;

public interface IGetSeoMetadataByIdUseCase
{
    Task<Result<GetSeoMetadataByIdResponse>> ExecuteAsync(
        GetSeoMetadataByIdRequest request,
        CancellationToken cancellationToken = default);
}