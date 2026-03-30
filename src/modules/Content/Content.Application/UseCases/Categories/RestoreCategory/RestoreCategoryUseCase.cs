using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Categories.RestoreCategory
{
    public sealed class RestoreCategoryUseCase : IRestoreCategoryUseCase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public RestoreCategoryUseCase(
            ICategoryRepository categoryRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _categoryRepository = categoryRepository;
            _dateTimeProvider = dateTimeProvider;
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

            var category = await _categoryRepository.GetByIdAsync(
                request.CategoryId,
                cancellationToken);

            if (category is null)
            {
                return Result<RestoreCategoryResponseDto>.Failure(
                    ContentErrors.Category.NotFound);
            }

            try
            {
                category.Restore(
                    nowUtc: _dateTimeProvider.UtcNow,
                    actorUserId: request.ActorUserId);

                var restoredCategory = await _categoryRepository.RestoreAsync(
                    request.CategoryId,
                    request.ActorUserId,
                    request.ExpectedVersion,
                    cancellationToken);

                if (restoredCategory is null)
                {
                    return Result<RestoreCategoryResponseDto>.Failure(
                        ContentErrors.ConcurrencyConflict);
                }

                var response = new RestoreCategoryResponseDto
                {
                    CategoryId = restoredCategory.CategoryId,
                    IsDeleted = restoredCategory.IsDeleted,
                    Version = restoredCategory.Version,
                    UpdatedAt = restoredCategory.UpdatedAt
                };

                return Result<RestoreCategoryResponseDto>.Success(response);
            }
            catch (PersistenceException exception)
            {
                return Result<RestoreCategoryResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (ContentDomainException exception)
            {
                return Result<RestoreCategoryResponseDto>.Failure(
                    MapDomainException(exception));
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
                _ => ContentErrors.ValidationFailed
            };
        }
    }
}