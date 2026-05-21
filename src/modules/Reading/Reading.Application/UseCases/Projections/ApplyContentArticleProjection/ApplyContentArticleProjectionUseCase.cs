using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Projections;

namespace Reading.Application.UseCases.Projections.ApplyContentArticleProjection;

public sealed class ApplyContentArticleProjectionUseCase
    : IApplyContentArticleProjectionUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;
    private readonly IReadingUnitOfWork _unitOfWork;

    public ApplyContentArticleProjectionUseCase(
        IArticleReadModelRepository articleReadModelRepository,
        IReadingUnitOfWork unitOfWork)
    {
        _articleReadModelRepository = articleReadModelRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyContentArticleProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            ApplyContentArticleProjectionValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        ApplyContentArticleProjectionCommand normalizedCommand =
            ApplyContentArticleProjectionValidator.Normalize(command);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            ArticleProjectionApplyResult result =
                await _articleReadModelRepository.UpsertFromContentAsync(
                    normalizedCommand,
                    cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ArticleProjectionApplyResult>.Success(result);
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }

            return Result<ArticleProjectionApplyResult>.Failure(
                ReadingErrors.Projection.ProjectionApplyFailed);
        }
    }
}