using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Categories.UpdateCategory
{
    public sealed class UpdateCategoryUseCase : IUpdateCategoryUseCase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateCategoryUseCase(
            ICategoryRepository categoryRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _categoryRepository = categoryRepository;
            _dateTimeProvider = dateTimeProvider;
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

            var category = await _categoryRepository.GetByIdAsync(
                request.CategoryId,
                cancellationToken);

            if (category is null)
            {
                return Result<UpdateCategoryResponseDto>.Failure(
                    ContentErrors.Category.NotFound);
            }

            if (request.ParentCategoryId.HasValue)
            {
                var parentCategory = await _categoryRepository.GetByIdAsync(
                    request.ParentCategoryId.Value,
                    cancellationToken);

                if (parentCategory is null || parentCategory.IsDeleted)
                {
                    return Result<UpdateCategoryResponseDto>.Failure(
                        ContentErrors.Category.ParentNotFound);
                }
            }

            var wouldCreateCycle = await WouldCreateCycleAsync(
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
                var nameNormalized = NormalizeName(request.Name);

                category.Update(
                    parentCategoryId: request.ParentCategoryId,
                    name: request.Name,
                    nameNormalized: nameNormalized,
                    description: request.Description,
                    isActive: request.IsActive,
                    displayOrder: request.DisplayOrder,
                    nowUtc: _dateTimeProvider.UtcNow,
                    actorUserId: request.ActorUserId);

                var updatedCategory = await _categoryRepository.UpdateAsync(
                    category,
                    request.ExpectedVersion,
                    cancellationToken);

                if (updatedCategory is null)
                {
                    return Result<UpdateCategoryResponseDto>.Failure(
                        ContentErrors.ConcurrencyConflict);
                }

                var response = new UpdateCategoryResponseDto
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
                };

                return Result<UpdateCategoryResponseDto>.Success(response);
            }
            catch (PersistenceException exception)
            {
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

            var visited = new HashSet<long>();
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

                var currentCategory = await _categoryRepository.GetByIdAsync(
                    currentCategoryId.Value,
                    cancellationToken);

                if (currentCategory is null)
                {
                    return false;
                }

                currentCategoryId = currentCategory.ParentCategoryId;
            }

            return false;
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
                _ => ContentErrors.ValidationFailed
            };
        }
    }
}