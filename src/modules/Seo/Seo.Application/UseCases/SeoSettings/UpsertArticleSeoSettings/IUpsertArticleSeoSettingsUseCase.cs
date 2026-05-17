using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Requests;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.UpsertArticleSeoSettings;

public interface IUpsertArticleSeoSettingsUseCase
{
    Task<Result<UpsertArticleSeoSettingsResponse>> ExecuteAsync(
        string articlePublicId,
        UpsertArticleSeoSettingsRequest request,
        CancellationToken cancellationToken = default);
}