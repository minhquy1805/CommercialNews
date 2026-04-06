using CommercialNews.BuildingBlocks.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;

namespace Reading.Application.UseCases.GetArticleBySlug;

/// <summary>
/// Handles the public hot path of opening an article by slug.
///
/// Responsibilities:
/// - resolve slug routing through SEO
/// - validate public visibility against Content truth
/// - compose the final public detail response
///
/// Notes:
/// - Route resolution is not serve authority.
/// - A resolved slug must still pass truth-backed visibility checks.
/// - This use case is the most important hot path in Reading V1.
/// </summary>
public interface IGetArticleBySlugUseCase
{
    Task<Result<GetArticleBySlugResponse>> ExecuteAsync(
        GetArticleBySlugRequest request,
        CancellationToken cancellationToken = default);
}