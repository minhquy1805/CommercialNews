using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;

namespace Reading.Application.UseCases.GetRelatedArticles;

/// <summary>
/// Handles deterministic related-article retrieval for Reading V1.
///
/// Responsibilities:
/// - exclude the current article
/// - apply deterministic related rules
/// - return only publicly readable related articles
///
/// Notes:
/// - Reading V1 related content is deterministic, not AI/ranking-driven.
/// - Rule order should remain: same category -> shared tags -> newest published fallback.
/// </summary>
public interface IGetRelatedArticlesUseCase
{
    Task<Result<GetRelatedArticlesResponse>> ExecuteAsync(
        GetRelatedArticlesRequest request,
        CancellationToken cancellationToken = default);
}