using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SeoMetadata.Responses;

namespace Seo.Application.UseCases.SeoSettings.GetArticleSeoSettings;

public interface IGetArticleSeoSettingsUseCase
{
    Task<Result<GetArticleSeoSettingsResponse>> ExecuteAsync(
        string articlePublicId,
        string? scope = null,
        CancellationToken cancellationToken = default);
}