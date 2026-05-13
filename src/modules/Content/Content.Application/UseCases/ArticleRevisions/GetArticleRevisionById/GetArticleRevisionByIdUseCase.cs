using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;

namespace Content.Application.UseCases.ArticleRevisions.GetArticleRevisionById;

public sealed class GetArticleRevisionByIdUseCase : IGetArticleRevisionByIdUseCase
{
    private readonly IArticleRevisionRepository _articleRevisionRepository;

    public GetArticleRevisionByIdUseCase(
        IArticleRevisionRepository articleRevisionRepository)
    {
        _articleRevisionRepository = articleRevisionRepository
            ?? throw new ArgumentNullException(nameof(articleRevisionRepository));
    }

    public async Task<Result<GetArticleRevisionByIdResponseDto>> ExecuteAsync(
        GetArticleRevisionByIdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ArticleId <= 0)
        {
            return Result<GetArticleRevisionByIdResponseDto>.Failure(
                ContentErrors.Article.InvalidArticleId);
        }

        if (request.RevisionId <= 0)
        {
            return Result<GetArticleRevisionByIdResponseDto>.Failure(
                ContentErrors.Revision.InvalidRevisionId);
        }

        ArticleRevision? revision = await _articleRevisionRepository.GetByIdAsync(
            articleId: request.ArticleId,
            revisionId: request.RevisionId,
            cancellationToken: cancellationToken);

        if (revision is null)
        {
            return Result<GetArticleRevisionByIdResponseDto>.Failure(
                ContentErrors.Revision.NotFound);
        }

        return Result<GetArticleRevisionByIdResponseDto>.Success(
            new GetArticleRevisionByIdResponseDto
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
            });
    }
}