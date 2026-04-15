using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.GenerateSlug;

public interface IGenerateSlugUseCase
{
    Task<Result<GenerateSlugResponse>> ExecuteAsync(
        GenerateSlugRequest request,
        CancellationToken cancellationToken = default);
}