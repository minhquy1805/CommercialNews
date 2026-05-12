using CommercialNews.BuildingBlocks.SharedKernel.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Categories.GetCategoryById;

public sealed class GetCategoryByIdUseCase : IGetCategoryByIdUseCase
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByIdUseCase(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public async Task<Result<GetCategoryByIdResponseDto>> ExecuteAsync(
        GetCategoryByIdRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId <= 0)
        {
            return Result<GetCategoryByIdResponseDto>.Failure(
                ContentErrors.Category.InvalidCategoryId);
        }

        var category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            cancellationToken);

        if (category is null)
        {
            return Result<GetCategoryByIdResponseDto>.Failure(
                ContentErrors.Category.NotFound);
        }

        return Result<GetCategoryByIdResponseDto>.Success(
            new GetCategoryByIdResponseDto
            {
                CategoryId = category.CategoryId,
                PublicId = category.PublicId,
                ParentCategoryId = category.ParentCategoryId,
                Name = category.Name,
                NameNormalized = category.NameNormalized,
                Description = category.Description,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                IsDeleted = category.IsDeleted,
                Version = category.Version,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                DeletedAt = category.DeletedAt
            });
    }
}