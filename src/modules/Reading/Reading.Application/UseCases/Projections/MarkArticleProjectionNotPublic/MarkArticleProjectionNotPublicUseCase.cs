using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Projections;
using Reading.Domain.Exceptions;

namespace Reading.Application.UseCases.Projections.MarkArticleProjectionNotPublic;

public sealed class MarkArticleProjectionNotPublicUseCase
    : IMarkArticleProjectionNotPublicUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;
    private readonly IReadingUnitOfWork _unitOfWork;

    public MarkArticleProjectionNotPublicUseCase(
        IArticleReadModelRepository articleReadModelRepository,
        IReadingUnitOfWork unitOfWork)
    {
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        MarkArticleProjectionNotPublicCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            MarkArticleProjectionNotPublicValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        MarkArticleProjectionNotPublicCommand normalizedCommand =
            MarkArticleProjectionNotPublicValidator.Normalize(command);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            ArticleProjectionApplyResult result =
                await _articleReadModelRepository.MarkNotPublicAsync(
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