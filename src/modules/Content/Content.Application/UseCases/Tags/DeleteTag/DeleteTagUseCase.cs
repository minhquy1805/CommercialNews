using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Tags.DeleteTag
{
    public sealed class DeleteTagUseCase : IDeleteTagUseCase
    {
        private readonly ITagRepository _tagRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public DeleteTagUseCase(
            ITagRepository tagRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _tagRepository = tagRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<DeleteTagResponseDto>> ExecuteAsync(
            DeleteTagRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.TagId <= 0)
            {
                return Result<DeleteTagResponseDto>.Failure(
                    ContentErrors.Tag.InvalidTagId);
            }

            if (request.ExpectedVersion <= 0)
            {
                return Result<DeleteTagResponseDto>.Failure(
                    ContentErrors.Tag.InvalidVersion);
            }

            var tag = await _tagRepository.GetByIdAsync(
                request.TagId,
                cancellationToken);

            if (tag is null)
            {
                return Result<DeleteTagResponseDto>.Failure(
                    ContentErrors.Tag.NotFound);
            }

            try
            {
                tag.SoftDelete(
                    nowUtc: _dateTimeProvider.UtcNow,
                    actorUserId: request.ActorUserId);

                var deletedTag = await _tagRepository.SoftDeleteAsync(
                    request.TagId,
                    request.ActorUserId,
                    request.ExpectedVersion,
                    cancellationToken);

                if (deletedTag is null)
                {
                    return Result<DeleteTagResponseDto>.Failure(
                        ContentErrors.ConcurrencyConflict);
                }

                var response = new DeleteTagResponseDto
                {
                    TagId = deletedTag.TagId,
                    IsDeleted = deletedTag.IsDeleted,
                    Version = deletedTag.Version,
                    DeletedAt = deletedTag.DeletedAt
                };

                return Result<DeleteTagResponseDto>.Success(response);
            }
            catch (PersistenceException exception)
            {
                return Result<DeleteTagResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (ContentDomainException exception)
            {
                return Result<DeleteTagResponseDto>.Failure(
                    MapDomainException(exception));
            }
        }

        private static Error MapDomainException(ContentDomainException exception)
        {
            return exception.Code switch
            {
                "CONTENT.TAG_ALREADY_DELETED" => ContentErrors.Tag.AlreadyDeleted,
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