using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Reading.Application.Errors;
using Reading.Application.Models.Commands;
using Reading.Application.Models.Results;
using Reading.Application.Ports.Persistence;
using Reading.Application.Validation.Projections;
using Reading.Domain.Exceptions;

namespace Reading.Application.UseCases.Projections.ApplyArticleMediaProjection;

public sealed class ApplyArticleMediaProjectionUseCase
    : IApplyArticleMediaProjectionUseCase
{
    private readonly IArticleReadModelRepository _articleReadModelRepository;
    private readonly IReadingUnitOfWork _unitOfWork;

    public ApplyArticleMediaProjectionUseCase(
        IArticleReadModelRepository articleReadModelRepository,
        IReadingUnitOfWork unitOfWork)
    {
        _articleReadModelRepository = articleReadModelRepository
            ?? throw new ArgumentNullException(nameof(articleReadModelRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<ArticleProjectionApplyResult>> UpsertAttachmentAsync(
        UpsertArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            ArticleMediaProjectionValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        UpsertArticleMediaProjectionCommand normalizedCommand =
            ArticleMediaProjectionValidator.Normalize(command);

        return await ExecuteAsync(
            () => _articleReadModelRepository.UpsertMediaAttachmentAsync(
                normalizedCommand,
                cancellationToken),
            cancellationToken);
    }

    public async Task<Result<ArticleProjectionApplyResult>> SetPrimaryAsync(
        SetPrimaryArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            ArticleMediaProjectionValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        SetPrimaryArticleMediaProjectionCommand normalizedCommand =
            ArticleMediaProjectionValidator.Normalize(command);

        return await ExecuteAsync(
            () => _articleReadModelRepository.SetPrimaryMediaAsync(
                normalizedCommand,
                cancellationToken),
            cancellationToken);
    }

    public async Task<Result<ArticleProjectionApplyResult>> ReorderAsync(
        ReorderArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            ArticleMediaProjectionValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        ReorderArticleMediaProjectionCommand normalizedCommand =
            ArticleMediaProjectionValidator.Normalize(command);

        return await ExecuteAsync(
            () => _articleReadModelRepository.ReorderMediaAsync(
                normalizedCommand,
                cancellationToken),
            cancellationToken);
    }

    public async Task<Result<ArticleProjectionApplyResult>> DetachAsync(
        DetachArticleMediaProjectionCommand command,
        CancellationToken cancellationToken = default)
    {
        Error? validationError =
            ArticleMediaProjectionValidator.Validate(command);

        if (validationError is not null)
        {
            return Result<ArticleProjectionApplyResult>.Failure(
                validationError);
        }

        DetachArticleMediaProjectionCommand normalizedCommand =
            ArticleMediaProjectionValidator.Normalize(command);

        return await ExecuteAsync(
            () => _articleReadModelRepository.DetachMediaAsync(
                normalizedCommand,
                cancellationToken),
            cancellationToken);
    }

    private async Task<Result<ArticleProjectionApplyResult>> ExecuteAsync(
        Func<Task<ArticleProjectionApplyResult>> apply,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            ArticleProjectionApplyResult result = await apply();

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