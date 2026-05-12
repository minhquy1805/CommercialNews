using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Tags.RestoreTag;

public sealed class RestoreTagUseCase : IRestoreTagUseCase
{
    private readonly ITagRepository _tagRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreTagUseCase(
        ITagRepository tagRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
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

        Tag? tag = await _tagRepository.GetByIdAsync(
            request.TagId,
            cancellationToken);

        if (tag is null)
        {
            return Result<RestoreTagResponseDto>.Failure(
                ContentErrors.Tag.NotFound);
        }

        if (tag.Version != request.ExpectedVersion)
        {
            return Result<RestoreTagResponseDto>.Failure(
                ContentErrors.ConcurrencyConflict);
        }

        try
        {
            tag.Restore(
                nowUtc: _dateTimeProvider.UtcNow,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Tag? restoredTag = await _tagRepository.RestoreAsync(
                tagId: request.TagId,
                updatedByUserId: request.ActorUserId,
                expectedVersion: request.ExpectedVersion,
                cancellationToken: cancellationToken);

            if (restoredTag is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<RestoreTagResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<RestoreTagResponseDto>.Success(
                new RestoreTagResponseDto
                {
                    TagId = restoredTag.TagId,
                    IsDeleted = restoredTag.IsDeleted,
                    IsActive = restoredTag.IsActive,
                    Version = restoredTag.Version,
                    UpdatedAt = restoredTag.UpdatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<RestoreTagResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<RestoreTagResponseDto>.Failure(
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
            _ => ContentErrors.WriteCommitFailed
        };
    }
}
