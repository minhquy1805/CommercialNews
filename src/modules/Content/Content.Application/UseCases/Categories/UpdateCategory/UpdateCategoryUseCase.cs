using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Categories.UpdateCategory;

public sealed class UpdateCategoryUseCase : IUpdateCategoryUseCase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCategoryUseCase(
        ICategoryRepository categoryRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<UpdateCategoryResponseDto>> ExecuteAsync(
        UpdateCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId <= 0)
        {
            return Result<UpdateCategoryResponseDto>.Failure(
                ContentErrors.Category.InvalidCategoryId);
        }

        if (request.ExpectedVersion <= 0)
        {
            return Result<UpdateCategoryResponseDto>.Failure(
                ContentErrors.Category.InvalidVersion);
        }

        Category? category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            cancellationToken);

        if (category is null)
        {
            return Result<UpdateCategoryResponseDto>.Failure(
                ContentErrors.Category.NotFound);
        }

        if (category.Version != request.ExpectedVersion)
        {
            return Result<UpdateCategoryResponseDto>.Failure(
                ContentErrors.ConcurrencyConflict);
        }

        string nameNormalized = NormalizeName(request.Name);

        bool nameExists = await _categoryRepository.ExistsByNameNormalizedAsync(
            nameNormalized,
            excludingCategoryId: request.CategoryId,
            cancellationToken);

        if (nameExists)
        {
            return Result<UpdateCategoryResponseDto>.Failure(
                ContentErrors.Category.NameNormalizedAlreadyExists);
        }

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value <= 0)
            {
                return Result<UpdateCategoryResponseDto>.Failure(
                    ContentErrors.Category.ParentIdInvalid);
            }

            Category? parentCategory = await _categoryRepository.GetByIdAsync(
                request.ParentCategoryId.Value,
                cancellationToken);

            if (parentCategory is null || parentCategory.IsDeleted)
            {
                return Result<UpdateCategoryResponseDto>.Failure(
                    ContentErrors.Category.ParentNotFound);
            }
        }

        bool wouldCreateCycle = await WouldCreateCycleAsync(
            request.CategoryId,
            request.ParentCategoryId,
            cancellationToken);

        if (wouldCreateCycle)
        {
            return Result<UpdateCategoryResponseDto>.Failure(
                ContentErrors.Category.CycleDetected);
        }

        try
        {
            category.Update(
                parentCategoryId: request.ParentCategoryId,
                name: request.Name,
                nameNormalized: nameNormalized,
                description: request.Description,
                isActive: request.IsActive,
                displayOrder: request.DisplayOrder,
                nowUtc: _dateTimeProvider.UtcNow,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Category? updatedCategory = await _categoryRepository.UpdateAsync(
                category,
                request.ExpectedVersion,
                cancellationToken);

            if (updatedCategory is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<UpdateCategoryResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<UpdateCategoryResponseDto>.Success(
                new UpdateCategoryResponseDto
                {
                    CategoryId = updatedCategory.CategoryId,
                    ParentCategoryId = updatedCategory.ParentCategoryId,
                    Name = updatedCategory.Name,
                    NameNormalized = updatedCategory.NameNormalized,
                    Description = updatedCategory.Description,
                    IsActive = updatedCategory.IsActive,
                    DisplayOrder = updatedCategory.DisplayOrder,
                    Version = updatedCategory.Version,
                    UpdatedAt = updatedCategory.UpdatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<UpdateCategoryResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<UpdateCategoryResponseDto>.Failure(
                MapDomainException(exception));
        }
    }

    private async Task<bool> WouldCreateCycleAsync(
        long categoryId,
        long? proposedParentCategoryId,
        CancellationToken cancellationToken)
    {
        if (!proposedParentCategoryId.HasValue)
        {
            return false;
        }

        HashSet<long> visited = [];
        long? currentCategoryId = proposedParentCategoryId;

        while (currentCategoryId.HasValue)
        {
            if (currentCategoryId.Value == categoryId)
            {
                return true;
            }

            if (!visited.Add(currentCategoryId.Value))
            {
                return true;
            }

            Category? currentCategory = await _categoryRepository.GetByIdAsync(
                currentCategoryId.Value,
                cancellationToken);

            if (currentCategory is null || currentCategory.IsDeleted)
            {
                return false;
            }

            currentCategoryId = currentCategory.ParentCategoryId;
        }

        return false;
    }

    private async Task RollbackIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
        }
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static Error MapDomainException(ContentDomainException exception)
    {
        return exception.Code switch
        {
            "CONTENT.CATEGORY_NAME_REQUIRED" => ContentErrors.Category.NameRequired,
            "CONTENT.CATEGORY_NAME_TOO_LONG" => ContentErrors.Category.NameTooLong,
            "CONTENT.CATEGORY_NAME_NORMALIZED_REQUIRED" => ContentErrors.Category.NameNormalizedRequired,
            "CONTENT.CATEGORY_NAME_NORMALIZED_TOO_LONG" => ContentErrors.Category.NameNormalizedTooLong,
            "CONTENT.CATEGORY_DISPLAY_ORDER_INVALID" => ContentErrors.Category.DisplayOrderInvalid,
            "CONTENT.CATEGORY_PARENT_ID_INVALID" => ContentErrors.Category.ParentIdInvalid,
            "CONTENT.CATEGORY_PARENT_SELF_REFERENCE" => ContentErrors.Category.ParentSelfReference,
            "CONTENT.CATEGORY_ALREADY_DELETED" => ContentErrors.Category.AlreadyDeleted,
            _ => ContentErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
            "CONTENT.CATEGORY_PARENT_NOT_FOUND" => ContentErrors.Category.ParentNotFound,
            "CONTENT.CATEGORY_NAME_NORMALIZED_ALREADY_EXISTS" => ContentErrors.Category.NameNormalizedAlreadyExists,
            "CONTENT.CATEGORY_CONFLICT" => ContentErrors.Category.Conflict,
            _ => ContentErrors.WriteCommitFailed
        };
    }
}