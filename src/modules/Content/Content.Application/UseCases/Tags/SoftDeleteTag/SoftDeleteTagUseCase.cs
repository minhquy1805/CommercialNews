using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Tags.SoftDeleteTag;

public sealed class SoftDeleteTagUseCase : ISoftDeleteTagUseCase
{
    private readonly ITagRepository _tagRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SoftDeleteTagUseCase(
        ITagRepository tagRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<SoftDeleteTagResponseDto>> ExecuteAsync(
        SoftDeleteTagRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.TagId <= 0)
        {
            return Result<SoftDeleteTagResponseDto>.Failure(
                ContentErrors.Tag.InvalidTagId);
        }

        if (request.ExpectedVersion <= 0)
        {
            return Result<SoftDeleteTagResponseDto>.Failure(
                ContentErrors.Tag.InvalidVersion);
        }

        Tag? tag = await _tagRepository.GetByIdAsync(
            request.TagId,
            cancellationToken);

        if (tag is null)
        {
            return Result<SoftDeleteTagResponseDto>.Failure(
                ContentErrors.Tag.NotFound);
        }

        if (tag.Version != request.ExpectedVersion)
        {
            return Result<SoftDeleteTagResponseDto>.Failure(
                ContentErrors.ConcurrencyConflict);
        }

        try
        {
            tag.SoftDelete(
                nowUtc: _dateTimeProvider.UtcNow,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Tag? softDeletedTag = await _tagRepository.SoftDeleteAsync(
                tagId: request.TagId,
                deletedByUserId: request.ActorUserId,
                expectedVersion: request.ExpectedVersion,
                cancellationToken: cancellationToken);

            if (softDeletedTag is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<SoftDeleteTagResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<SoftDeleteTagResponseDto>.Success(
                new SoftDeleteTagResponseDto
                {
                    TagId = softDeletedTag.TagId,
                    IsDeleted = softDeletedTag.IsDeleted,
                    IsActive = softDeletedTag.IsActive,
                    Version = softDeletedTag.Version,
                    UpdatedAt = softDeletedTag.UpdatedAt,
                    DeletedAt = softDeletedTag.DeletedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<SoftDeleteTagResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<SoftDeleteTagResponseDto>.Failure(
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
            "CONTENT.TAG_ALREADY_DELETED" => ContentErrors.Tag.AlreadyDeleted,
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
