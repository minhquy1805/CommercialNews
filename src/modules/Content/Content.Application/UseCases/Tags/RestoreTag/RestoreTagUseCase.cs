using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Tags.RestoreTag
{
    public sealed class RestoreTagUseCase : IRestoreTagUseCase
    {
        private readonly ITagRepository _tagRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public RestoreTagUseCase(
            ITagRepository tagRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _tagRepository = tagRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<RestoreTagResponseDto>> ExecuteAsync(
            RestoreTagRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.TagId <= 0)
            {
                return Result<RestoreTagResponseDto>.Failure(
                    ContentErrors.Tag.InvalidTagId);
            }

            if (request.ExpectedVersion <= 0)
            {
                return Result<RestoreTagResponseDto>.Failure(
                    ContentErrors.Tag.InvalidVersion);
            }

            var tag = await _tagRepository.GetByIdAsync(
                request.TagId,
                cancellationToken);

            if (tag is null)
            {
                return Result<RestoreTagResponseDto>.Failure(
                    ContentErrors.Tag.NotFound);
            }

            try
            {
                tag.Restore(
                    nowUtc: _dateTimeProvider.UtcNow,
                    actorUserId: request.ActorUserId);

                var restoredTag = await _tagRepository.RestoreAsync(
                    request.TagId,
                    request.ActorUserId,
                    request.ExpectedVersion,
                    cancellationToken);

                if (restoredTag is null)
                {
                    return Result<RestoreTagResponseDto>.Failure(
                        ContentErrors.ConcurrencyConflict);
                }

                var response = new RestoreTagResponseDto
                {
                    TagId = restoredTag.TagId,
                    IsDeleted = restoredTag.IsDeleted,
                    Version = restoredTag.Version,
                    UpdatedAt = restoredTag.UpdatedAt
                };

                return Result<RestoreTagResponseDto>.Success(response);
            }
            catch (PersistenceException exception)
            {
                return Result<RestoreTagResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (ContentDomainException exception)
            {
                return Result<RestoreTagResponseDto>.Failure(
                    MapDomainException(exception));
            }
        }

        private static Error MapDomainException(ContentDomainException exception)
        {
            return exception.Code switch
            {
                "CONTENT.TAG_NOT_DELETED" => ContentErrors.Tag.NotDeleted,
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