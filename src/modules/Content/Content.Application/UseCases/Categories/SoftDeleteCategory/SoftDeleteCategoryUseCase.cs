using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Categories.SoftDeleteCategory;

public sealed class SoftDeleteCategoryUseCase : ISoftDeleteCategoryUseCase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SoftDeleteCategoryUseCase(
        ICategoryRepository categoryRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<SoftDeleteCategoryResponseDto>> ExecuteAsync(
        SoftDeleteCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId <= 0)
        {
            return Result<SoftDeleteCategoryResponseDto>.Failure(
                ContentErrors.Category.InvalidCategoryId);
        }

        if (request.ExpectedVersion <= 0)
        {
            return Result<SoftDeleteCategoryResponseDto>.Failure(
                ContentErrors.Category.InvalidVersion);
        }

        Category? category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            cancellationToken);

        if (category is null)
        {
            return Result<SoftDeleteCategoryResponseDto>.Failure(
                ContentErrors.Category.NotFound);
        }

        if (category.Version != request.ExpectedVersion)
        {
            return Result<SoftDeleteCategoryResponseDto>.Failure(
                ContentErrors.ConcurrencyConflict);
        }

        try
        {
            category.SoftDelete(
                nowUtc: _dateTimeProvider.UtcNow,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Category? softDeletedCategory = await _categoryRepository.SoftDeleteAsync(
                categoryId: request.CategoryId,
                deletedByUserId: request.ActorUserId,
                expectedVersion: request.ExpectedVersion,
                cancellationToken: cancellationToken);

            if (softDeletedCategory is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<SoftDeleteCategoryResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<SoftDeleteCategoryResponseDto>.Success(
                new SoftDeleteCategoryResponseDto
                {
                    CategoryId = softDeletedCategory.CategoryId,
                    IsDeleted = softDeletedCategory.IsDeleted,
                    IsActive = softDeletedCategory.IsActive,
                    Version = softDeletedCategory.Version,
                    UpdatedAt = softDeletedCategory.UpdatedAt,
                    DeletedAt = softDeletedCategory.DeletedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<SoftDeleteCategoryResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<SoftDeleteCategoryResponseDto>.Failure(
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
            "CONTENT.CATEGORY_ALREADY_DELETED" => ContentErrors.Category.AlreadyDeleted,
            _ => ContentErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
            "CONTENT.CATEGORY_DELETE_BLOCKED_BY_ARTICLES" => ContentErrors.Category.DeleteBlockedByArticles,
            _ => ContentErrors.WriteCommitFailed
        };
    }
}