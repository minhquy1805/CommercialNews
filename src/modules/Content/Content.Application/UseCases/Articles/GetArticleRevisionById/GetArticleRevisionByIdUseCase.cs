using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Models.QueryModels;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Articles.GetArticleRevisionById
{
    public sealed class GetArticleRevisionByIdUseCase : IGetArticleRevisionByIdUseCase
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IArticleRevisionRepository _articleRevisionRepository;

        public GetArticleRevisionByIdUseCase(
            IArticleRepository articleRepository,
            IArticleRevisionRepository articleRevisionRepository)
        {
            _articleRepository = articleRepository;
            _articleRevisionRepository = articleRevisionRepository;
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

            var article = await _articleRepository.GetByIdAsync(
                request.ArticleId,
                cancellationToken);

            if (article is null)
            {
                return Result<GetArticleRevisionByIdResponseDto>.Failure(
                    ContentErrors.Article.NotFound);
            }

            ArticleRevisionDetailResultItem? revision = await _articleRevisionRepository.GetByIdAsync(
                request.ArticleId,
                request.RevisionId,
                cancellationToken);

            if (revision is null)
            {
                return Result<GetArticleRevisionByIdResponseDto>.Failure(
                    ContentErrors.Revision.NotFound);
            }

            return Result<GetArticleRevisionByIdResponseDto>.Success(new GetArticleRevisionByIdResponseDto
            {
                RevisionId = revision.RevisionId,
                ArticleId = revision.ArticleId,
                RevisionNumber = revision.RevisionNumber,
                TitleSnapshot = revision.TitleSnapshot,
                SummarySnapshot = revision.SummarySnapshot,
                BodySnapshot = revision.BodySnapshot,
                CategoryIdSnapshot = revision.CategoryIdSnapshot,
                StatusSnapshot = revision.StatusSnapshot,
                CoverMediaIdSnapshot = revision.CoverMediaIdSnapshot,
                ChangedAt = revision.ChangedAt,
                ChangedByUserId = revision.ChangedByUserId,
                ChangeType = revision.ChangeType,
                ChangeSummary = revision.ChangeSummary
            });
        }
    }
}

