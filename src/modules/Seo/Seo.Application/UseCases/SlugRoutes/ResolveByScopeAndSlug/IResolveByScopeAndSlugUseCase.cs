using CommercialNews.BuildingBlocks.Results;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;

namespace Seo.Application.UseCases.SlugRoutes.ResolveByScopeAndSlug;

public interface IResolveByScopeAndSlugUseCase
{
    Task<Result<ResolveByScopeAndSlugResponse>> ExecuteAsync(
        ResolveByScopeAndSlugRequest request,
        CancellationToken cancellationToken = default);
}