using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Categories.CreateCategory;

public sealed class CreateCategoryUseCase : ICreateCategoryUseCase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCategoryUseCase(
        ICategoryRepository categoryRepository,
        IContentUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publicIdGenerator = publicIdGenerator ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<CreateCategoryResponseDto>> ExecuteAsync(
        CreateCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string nameNormalized = NormalizeName(request.Name);

            if (request.ParentCategoryId.HasValue)
            {
                Category? parentCategory = await _categoryRepository.GetByIdAsync(
                    request.ParentCategoryId.Value,
                    cancellationToken);

                if (parentCategory is null || parentCategory.IsDeleted)
                {
                    return Result<CreateCategoryResponseDto>.Failure(
                        ContentErrors.Category.ParentNotFound);
                }
            }

            bool nameExists = await _categoryRepository.ExistsByNameNormalizedAsync(
                nameNormalized,
                excludingCategoryId: null,
                cancellationToken);

            if (nameExists)
            {
                return Result<CreateCategoryResponseDto>.Failure(
                    ContentErrors.Category.NameNormalizedAlreadyExists);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            string publicId = _publicIdGenerator.NewId();

            Category category = Category.Create(
                publicId: publicId,
                parentCategoryId: request.ParentCategoryId,
                name: request.Name,
                nameNormalized: nameNormalized,
                description: request.Description,
                isActive: request.IsActive,
                displayOrder: request.DisplayOrder,
                nowUtc: nowUtc,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Category? createdCategory = await _categoryRepository.InsertAsync(
                category,
                cancellationToken);

            if (createdCategory is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<CreateCategoryResponseDto>.Failure(
                    ContentErrors.WriteCommitFailed);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<CreateCategoryResponseDto>.Success(
                new CreateCategoryResponseDto
                {
                    CategoryId = createdCategory.CategoryId,
                    PublicId = createdCategory.PublicId,
                    ParentCategoryId = createdCategory.ParentCategoryId,
                    Name = createdCategory.Name,
                    NameNormalized = createdCategory.NameNormalized,
                    Description = createdCategory.Description,
                    IsActive = createdCategory.IsActive,
                    DisplayOrder = createdCategory.DisplayOrder,
                    Version = createdCategory.Version,
                    CreatedAt = createdCategory.CreatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<CreateCategoryResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<CreateCategoryResponseDto>.Failure(
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
            "CONTENT.CATEGORY_PUBLIC_ID_REQUIRED" => ContentErrors.Category.PublicIdRequired,
            "CONTENT.CATEGORY_PUBLIC_ID_INVALID" => ContentErrors.Category.PublicIdInvalid,
            "CONTENT.CATEGORY_NAME_REQUIRED" => ContentErrors.Category.NameRequired,
            "CONTENT.CATEGORY_NAME_TOO_LONG" => ContentErrors.Category.NameTooLong,
            "CONTENT.CATEGORY_NAME_NORMALIZED_REQUIRED" => ContentErrors.Category.NameNormalizedRequired,
            "CONTENT.CATEGORY_NAME_NORMALIZED_TOO_LONG" => ContentErrors.Category.NameNormalizedTooLong,
            "CONTENT.CATEGORY_DISPLAY_ORDER_INVALID" => ContentErrors.Category.DisplayOrderInvalid,
            "CONTENT.CATEGORY_PARENT_SELF_REFERENCE" => ContentErrors.Category.ParentSelfReference,
            "CONTENT.CATEGORY_PARENT_ID_INVALID" => ContentErrors.Category.ParentIdInvalid,
            _ => ContentErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "CONTENT.CATEGORY_PARENT_NOT_FOUND" => ContentErrors.Category.ParentNotFound,
            "CONTENT.CATEGORY_NAME_NORMALIZED_ALREADY_EXISTS" => ContentErrors.Category.NameNormalizedAlreadyExists,
            "CONTENT.CATEGORY_CONFLICT" => ContentErrors.Category.Conflict,
            "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
            _ => ContentErrors.WriteCommitFailed
        };
    }
}