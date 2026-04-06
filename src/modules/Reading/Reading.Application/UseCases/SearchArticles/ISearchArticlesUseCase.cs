using CommercialNews.BuildingBlocks.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;

namespace Reading.Application.UseCases.SearchArticles;

/// <summary>
/// Handles public article search for Reading V1.
///
/// Responsibilities:
/// - validate keyword/paging/sort input
/// - search only publicly readable content
/// - return a paged public search result
///
/// Notes:
/// - Search result visibility must remain truth-backed.
/// - Search/materialized/query-accelerated data must never bypass visibility rules.
/// </summary>
public interface ISearchArticlesUseCase
{
    Task<Result<SearchArticlesResponse>> ExecuteAsync(
        SearchArticlesRequest request,
        CancellationToken cancellationToken = default);
}