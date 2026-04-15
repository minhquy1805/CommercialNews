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

namespace Content.Application.UseCases.Tags.CreateTag
{
    public sealed class CreateTagUseCase : ICreateTagUseCase
    {
        private readonly ITagRepository _tagRepository;
        private readonly IPublicIdGenerator _publicIdGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateTagUseCase(
            ITagRepository tagRepository,
            IPublicIdGenerator publicIdGenerator,
            IDateTimeProvider dateTimeProvider)
        {
            _tagRepository = tagRepository;
            _publicIdGenerator = publicIdGenerator;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<CreateTagResponseDto>> ExecuteAsync(
            CreateTagRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var nowUtc = _dateTimeProvider.UtcNow;
                var publicId = _publicIdGenerator.NewId();
                var nameNormalized = NormalizeName(request.Name);

                var tag = Tag.Create(
                    publicId: publicId,
                    name: request.Name,
                    nameNormalized: nameNormalized,
                    description: request.Description,
                    isActive: request.IsActive,
                    nowUtc: nowUtc,
                    actorUserId: request.ActorUserId);

                var createdTag = await _tagRepository.InsertAsync(
                    tag,
                    cancellationToken);

                if (createdTag is null)
                {
                    return Result<CreateTagResponseDto>.Failure(
                        ContentErrors.ValidationFailed);
                }

                var response = new CreateTagResponseDto
                {
                    TagId = createdTag.TagId,
                    PublicId = createdTag.PublicId,
                    Name = createdTag.Name,
                    NameNormalized = createdTag.NameNormalized,
                    Description = createdTag.Description,
                    IsActive = createdTag.IsActive,
                    Version = createdTag.Version,
                    CreatedAt = createdTag.CreatedAt
                };

                return Result<CreateTagResponseDto>.Success(response);
            }
            catch (PersistenceException exception)
            {
                return Result<CreateTagResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (ContentDomainException exception)
            {
                return Result<CreateTagResponseDto>.Failure(
                    MapDomainException(exception));
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
                "CONTENT.TAG_PUBLIC_ID_REQUIRED" => ContentErrors.Tag.PublicIdRequired,
                "CONTENT.TAG_PUBLIC_ID_INVALID" => ContentErrors.Tag.PublicIdInvalid,
                "CONTENT.TAG_NAME_REQUIRED" => ContentErrors.Tag.NameRequired,
                "CONTENT.TAG_NAME_TOO_LONG" => ContentErrors.Tag.NameTooLong,
                "CONTENT.TAG_NAME_NORMALIZED_REQUIRED" => ContentErrors.Tag.NameNormalizedRequired,
                "CONTENT.TAG_NAME_NORMALIZED_TOO_LONG" => ContentErrors.Tag.NameNormalizedTooLong,
                "CONTENT.TAG_DESCRIPTION_TOO_LONG" => ContentErrors.Tag.DescriptionTooLong,
                _ => ContentErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "CONTENT.TAG_PUBLIC_ID_REQUIRED" => ContentErrors.Tag.PublicIdRequired,
                "CONTENT.TAG_PUBLIC_ID_INVALID" => ContentErrors.Tag.PublicIdInvalid,
                "CONTENT.TAG_NAME_REQUIRED" => ContentErrors.Tag.NameRequired,
                "CONTENT.TAG_NAME_NORMALIZED_REQUIRED" => ContentErrors.Tag.NameNormalizedRequired,
                "CONTENT.TAG_NAME_NORMALIZED_ALREADY_EXISTS" => ContentErrors.Tag.NameNormalizedAlreadyExists,
                _ => ContentErrors.ValidationFailed
            };
        }
    }
}