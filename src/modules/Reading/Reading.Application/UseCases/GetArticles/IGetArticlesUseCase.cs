using CommercialNews.BuildingBlocks.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;

namespace Reading.Application.UseCases.GetArticles;

/// <summary>
/// Handles the public article listing flow for Reading V1.
///
/// Responsibilities:
/// - validate paging/filter/sort input
/// - normalize sort semantics for public reading
/// - return only truth-safe publicly readable articles
/// - allow optional enrichments to degrade safely
///
/// Notes:
/// - This use case must not depend on interaction/counter freshness for success.
/// - Visibility correctness must still come from truth-backed sources.
/// </summary>
public interface IGetArticlesUseCase
{
    Task<Result<GetArticlesResponse>> ExecuteAsync(
        GetArticlesRequest request,
        CancellationToken cancellationToken = default);
}