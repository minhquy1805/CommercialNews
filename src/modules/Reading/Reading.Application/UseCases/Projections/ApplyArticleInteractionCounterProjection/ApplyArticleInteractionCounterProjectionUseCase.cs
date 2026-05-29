using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Projections;
using Reading.Domain.Constants;
using Reading.Domain.Exceptions;

namespace Reading.Application.UseCases.Projections.ApplyArticleInteractionCounterProjection;

public sealed class ApplyArticleInteractionCounterProjectionUseCase
    : IApplyArticleInteractionCounterProjectionUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;
    private readonly IReadingUnitOfWork _unitOfWork;

    public ApplyArticleInteractionCounterProjectionUseCase(
        IArticleReadModelRepository articleReadModelRepository,
        IReadingUnitOfWork unitOfWork)
    {
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyArticleInteractionCounterProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            ApplyArticleInteractionCounterProjectionValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        ApplyArticleInteractionCounterProjectionCommand normalizedCommand =
            ApplyArticleInteractionCounterProjectionValidator.Normalize(command);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            ArticleProjectionApplyResult result =
                await _articleReadModelRepository.ApplyInteractionCountersAsync(
                    normalizedCommand,
                    cancellationToken);

            if (!ProjectionApplyDecisions.IsValid(result.Decision))
            {
                await RollbackIfNeededAsync(CancellationToken.None);

                return Result<ArticleProjectionApplyResult>.Failure(
                    ReadingErrors.Projection
                        .InteractionCounterProjectionApplyFailed);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<ArticleProjectionApplyResult>.Success(result);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            await RollbackIfNeededAsync(CancellationToken.None);
            throw;
        }
        catch (PersistenceException)
        {
            await RollbackIfNeededAsync(CancellationToken.None);

            return Result<ArticleProjectionApplyResult>.Failure(
                ReadingErrors.Projection.InteractionCounterProjectionApplyFailed);
        }
        catch (ReadingDomainException)
        {
            await RollbackIfNeededAsync(CancellationToken.None);

            return Result<ArticleProjectionApplyResult>.Failure(
                ReadingErrors.ValidationFailed);
        }
        catch
        {
            await RollbackIfNeededAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task RollbackIfNeededAsync(
        CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
        }
    }
}
