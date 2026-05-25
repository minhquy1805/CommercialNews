using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Projections;
using Reading.Domain.Exceptions;

namespace Reading.Application.UseCases.Projections.ApplyAuthorProfileProjection;

public sealed class ApplyAuthorProfileProjectionUseCase
    : IApplyAuthorProfileProjectionUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;
    private readonly IReadingUnitOfWork _unitOfWork;

    public ApplyAuthorProfileProjectionUseCase(
        IArticleReadModelRepository articleReadModelRepository,
        IReadingUnitOfWork unitOfWork)
    {
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        ApplyAuthorProfileProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            ApplyAuthorProfileProjectionValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        ApplyAuthorProfileProjectionCommand normalizedCommand =
            ApplyAuthorProfileProjectionValidator.Normalize(command);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            ArticleProjectionApplyResult result =
                await _articleReadModelRepository.ApplyAuthorProfileAsync(
                    normalizedCommand,
                    cancellationToken);

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
                ReadingErrors.Projection.ProjectionApplyFailed);
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