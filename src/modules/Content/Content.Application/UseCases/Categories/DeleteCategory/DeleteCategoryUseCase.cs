using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Categories.DeleteCategory
{
    public sealed class DeleteCategoryUseCase : IDeleteCategoryUseCase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public DeleteCategoryUseCase(
            ICategoryRepository categoryRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _categoryRepository = categoryRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<DeleteCategoryResponseDto>> ExecuteAsync(
            DeleteCategoryRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.CategoryId <= 0)
            {
                return Result<DeleteCategoryResponseDto>.Failure(
                    ContentErrors.Category.InvalidCategoryId);
            }

            if (request.ExpectedVersion <= 0)
            {
                return Result<DeleteCategoryResponseDto>.Failure(
                    ContentErrors.Category.InvalidVersion);
            }

            var category = await _categoryRepository.GetByIdAsync(
                request.CategoryId,
                cancellationToken);

            if (category is null)
            {
                return Result<DeleteCategoryResponseDto>.Failure(
                    ContentErrors.Category.NotFound);
            }

            try
            {
                category.SoftDelete(
                    nowUtc: _dateTimeProvider.UtcNow,
                    actorUserId: request.ActorUserId);

                var deletedCategory = await _categoryRepository.SoftDeleteAsync(
                    request.CategoryId,
                    request.ActorUserId,
                    request.ExpectedVersion,
                    cancellationToken);

                if (deletedCategory is null)
                {
                    return Result<DeleteCategoryResponseDto>.Failure(
                        ContentErrors.ConcurrencyConflict);
                }

                var response = new DeleteCategoryResponseDto
                {
                    CategoryId = deletedCategory.CategoryId,
                    IsDeleted = deletedCategory.IsDeleted,
                    Version = deletedCategory.Version,
                    DeletedAt = deletedCategory.DeletedAt
                };

                return Result<DeleteCategoryResponseDto>.Success(response);
            }
            catch (PersistenceException exception)
            {
                return Result<DeleteCategoryResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (ContentDomainException exception)
            {
                return Result<DeleteCategoryResponseDto>.Failure(
                    MapDomainException(exception));
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
                _ => ContentErrors.ValidationFailed
            };
        }
    }
}