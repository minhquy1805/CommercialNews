using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.GetSeoMetadataByArticleId;

public interface IGetSeoMetadataByArticleIdUseCase
{
    Task<Result<GetSeoMetadataByArticleIdResponse>> ExecuteAsync(
        GetSeoMetadataByArticleIdRequest request,
        CancellationToken cancellationToken = default);
}