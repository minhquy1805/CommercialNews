using CommercialNews.BuildingBlocks.Results;
using Reading.Application.Contracts.Requests;
using Reading.Application.Contracts.Responses;

namespace Reading.Application.UseCases.GetArticleById;

/// <summary>
/// Handles the public article detail flow by article id.
///
/// Responsibilities:
/// - load a public article by id
/// - enforce truth-safe visibility
/// - compose category, tags, media, seo, and optional counters
///
/// Notes:
/// - If the article is not publicly readable, the result must be a safe not-found.
/// - Optional enrichments must not turn a valid public read into a full failure.
/// </summary>
public interface IGetArticleByIdUseCase
{
    Task<Result<GetArticleByIdResponse>> ExecuteAsync(
        GetArticleByIdRequest request,
        CancellationToken cancellationToken = default);
}