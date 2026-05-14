using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Categories.RestoreCategory;

public sealed class RestoreCategoryUseCase : IRestoreCategoryUseCase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreCategoryUseCase(
        ICategoryRepository categoryRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<RestoreCategoryResponseDto>> ExecuteAsync(
        RestoreCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId <= 0)
        {
            return Result<RestoreCategoryResponseDto>.Failure(
                ContentErrors.Category.InvalidCategoryId);
        }

        if (request.ExpectedVersion <= 0)
        {
            return Result<RestoreCategoryResponseDto>.Failure(
                ContentErrors.Category.InvalidVersion);
        }

        Category? category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            cancellationToken);

        if (category is null)
        {
            return Result<RestoreCategoryResponseDto>.Failure(
                ContentErrors.Category.NotFound);
        }

        if (category.Version != request.ExpectedVersion)
        {
            return Result<RestoreCategoryResponseDto>.Failure(
                ContentErrors.ConcurrencyConflict);
        }

        try
        {
            category.Restore(
                nowUtc: _dateTimeProvider.UtcNow,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Category? restoredCategory = await _categoryRepository.RestoreAsync(
                categoryId: request.CategoryId,
                updatedByUserId: request.ActorUserId,
                expectedVersion: request.ExpectedVersion,
                cancellationToken: cancellationToken);

            if (restoredCategory is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<RestoreCategoryResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<RestoreCategoryResponseDto>.Success(
                new RestoreCategoryResponseDto
                {
                    CategoryId = restoredCategory.CategoryId,
                    IsDeleted = restoredCategory.IsDeleted,
                    IsActive = restoredCategory.IsActive,
                    Version = restoredCategory.Version,
                    UpdatedAt = restoredCategory.UpdatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<RestoreCategoryResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<RestoreCategoryResponseDto>.Failure(
                MapDomainException(exception));
        }
    }

    private async Task RollbackIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
        }
    }

    private static Error MapDomainException(ContentDomainException exception)
    {
        return exception.Code switch
        {
            "CONTENT.CATEGORY_NOT_DELETED" => ContentErrors.Category.NotDeleted,
            _ => ContentErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
            _ => ContentErrors.WriteCommitFailed
        };
    }
}