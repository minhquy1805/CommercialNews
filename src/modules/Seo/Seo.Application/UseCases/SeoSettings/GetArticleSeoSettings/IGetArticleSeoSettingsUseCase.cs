using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;

public interface IGetArticleSeoSettingsUseCase
{
    Task<Result<GetArticleSeoSettingsResponse>> ExecuteAsync(
        GetArticleSeoSettingsRequest request,
        CancellationToken cancellationToken = default);
}