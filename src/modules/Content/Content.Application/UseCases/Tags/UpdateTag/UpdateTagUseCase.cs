using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Tags.UpdateTag
{
    public sealed class UpdateTagUseCase : IUpdateTagUseCase
    {
        private readonly ITagRepository _tagRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateTagUseCase(
            ITagRepository tagRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _tagRepository = tagRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<UpdateTagResponseDto>> ExecuteAsync(
            UpdateTagRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.TagId <= 0)
            {
                return Result<UpdateTagResponseDto>.Failure(
                    ContentErrors.Tag.InvalidTagId);
            }

            if (request.ExpectedVersion <= 0)
            {
                return Result<UpdateTagResponseDto>.Failure(
                    ContentErrors.Tag.InvalidVersion);
            }

            var tag = await _tagRepository.GetByIdAsync(
                request.TagId,
                cancellationToken);

            if (tag is null)
            {
                return Result<UpdateTagResponseDto>.Failure(
                    ContentErrors.Tag.NotFound);
            }

            try
            {
                var nameNormalized = NormalizeName(request.Name);

                tag.Update(
                    name: request.Name,
                    nameNormalized: nameNormalized,
                    description: request.Description,
                    isActive: request.IsActive,
                    nowUtc: _dateTimeProvider.UtcNow,
                    actorUserId: request.ActorUserId);

                var updatedTag = await _tagRepository.UpdateAsync(
                    tag,
                    request.ExpectedVersion,
                    cancellationToken);

                if (updatedTag is null)
                {
                    return Result<UpdateTagResponseDto>.Failure(
                        ContentErrors.ConcurrencyConflict);
                }

                var response = new UpdateTagResponseDto
                {
                    TagId = updatedTag.TagId,
                    Name = updatedTag.Name,
                    NameNormalized = updatedTag.NameNormalized,
                    Description = updatedTag.Description,
                    IsActive = updatedTag.IsActive,
                    Version = updatedTag.Version,
                    UpdatedAt = updatedTag.UpdatedAt
                };

                return Result<UpdateTagResponseDto>.Success(response);
            }
            catch (PersistenceException exception)
            {
                return Result<UpdateTagResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (ContentDomainException exception)
            {
                return Result<UpdateTagResponseDto>.Failure(
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
                "CONTENT.TAG_NAME_REQUIRED" => ContentErrors.Tag.NameRequired,
                "CONTENT.TAG_NAME_TOO_LONG" => ContentErrors.Tag.NameTooLong,
                "CONTENT.TAG_NAME_NORMALIZED_REQUIRED" => ContentErrors.Tag.NameNormalizedRequired,
                "CONTENT.TAG_NAME_NORMALIZED_TOO_LONG" => ContentErrors.Tag.NameNormalizedTooLong,
                "CONTENT.TAG_DESCRIPTION_TOO_LONG" => ContentErrors.Tag.DescriptionTooLong,
                "CONTENT.TAG_ALREADY_DELETED" => ContentErrors.Tag.AlreadyDeleted,
                _ => ContentErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
                "CONTENT.TAG_NAME_NORMALIZED_ALREADY_EXISTS" => ContentErrors.Tag.NameNormalizedAlreadyExists,
                "CONTENT.TAG_INVALID_TAG_ID" => ContentErrors.Tag.InvalidTagId,
                "CONTENT.TAG_NAME_REQUIRED" => ContentErrors.Tag.NameRequired,
                "CONTENT.TAG_NAME_NORMALIZED_REQUIRED" => ContentErrors.Tag.NameNormalizedRequired,
                _ => ContentErrors.ValidationFailed
            };
        }
    }
}