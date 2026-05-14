using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;

namespace Content.Application.UseCases.ArticleRevisions.GetArticleRevisions;

public sealed class GetArticleRevisionsUseCase : IGetArticleRevisionsUseCase
{
    private readonly IArticleRevisionRepository _articleRevisionRepository;

    public GetArticleRevisionsUseCase(
        IArticleRevisionRepository articleRevisionRepository)
    {
        _articleRevisionRepository = articleRevisionRepository
            ?? throw new ArgumentNullException(nameof(articleRevisionRepository));
    }

    public async Task<Result<IReadOnlyList<ArticleRevisionItemDto>>> ExecuteAsync(
        GetArticleRevisionsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<IReadOnlyList<ArticleRevisionItemDto>>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        IReadOnlyList<ArticleRevision> revisions =
            await _articleRevisionRepository.GetByArticleIdAsync(
                request.ArticleId,
                cancellationToken);

        IReadOnlyList<ArticleRevisionItemDto> response = revisions
            .Select(static revision => new ArticleRevisionItemDto
            {
                RevisionId = revision.RevisionId,
                ArticleId = revision.ArticleId,
                EditedAt = revision.EditedAt,
                EditedByUserId = revision.EditedByUserId,
                ArticleVersion = revision.ArticleVersion,
                CorrelationId = revision.CorrelationId,
                ChangeSummary = revision.ChangeSummary,
                OldTitle = revision.OldTitle,
                OldSummary = revision.OldSummary,
                OldBody = revision.OldBody
            })
            .ToArray();

        return Result<IReadOnlyList<ArticleRevisionItemDto>>.Success(response);
    }
}