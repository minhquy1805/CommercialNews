using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;

namespace Content.Application.UseCases.Tags.GetTagById
{
    public sealed class GetTagByIdUseCase : IGetTagByIdUseCase
    {
        private readonly ITagRepository _tagRepository;

        public GetTagByIdUseCase(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<Result<GetTagByIdResponseDto>> ExecuteAsync(
            GetTagByIdRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.TagId <= 0)
            {
                return Result<GetTagByIdResponseDto>.Failure(
                    ContentErrors.Tag.InvalidTagId);
            }

            var tag = await _tagRepository.GetByIdAsync(
                request.TagId,
                cancellationToken);

            if (tag is null)
            {
                return Result<GetTagByIdResponseDto>.Failure(
                    ContentErrors.Tag.NotFound);
            }

            var response = new GetTagByIdResponseDto
            {
                TagId = tag.TagId,
                PublicId = tag.PublicId,
                Name = tag.Name,
                NameNormalized = tag.NameNormalized,
                Description = tag.Description,
                IsActive = tag.IsActive,
                IsDeleted = tag.IsDeleted,
                Version = tag.Version,
                CreatedAt = tag.CreatedAt,
                UpdatedAt = tag.UpdatedAt,
                DeletedAt = tag.DeletedAt
            };

            return Result<GetTagByIdResponseDto>.Success(response);
        }
    }
}