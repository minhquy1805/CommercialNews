using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Content.Application.Contracts.Requests;
using Content.Application.Contracts.Responses;
using Content.Application.Errors;
using Content.Application.Ports.Persistence;
using Content.Domain.Entities;
using Content.Domain.Exceptions;

namespace Content.Application.UseCases.Tags.UpdateTag;

public sealed class UpdateTagUseCase : IUpdateTagUseCase
{
    private readonly ITagRepository _tagRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateTagUseCase(
        ITagRepository tagRepository,
        IContentUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
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

        Tag? tag = await _tagRepository.GetByIdAsync(
            request.TagId,
            cancellationToken);

        if (tag is null)
        {
            return Result<UpdateTagResponseDto>.Failure(
                ContentErrors.Tag.NotFound);
        }

        if (tag.Version != request.ExpectedVersion)
        {
            return Result<UpdateTagResponseDto>.Failure(
                ContentErrors.ConcurrencyConflict);
        }

        string nameNormalized = NormalizeName(request.Name);

        bool nameExists = await _tagRepository.ExistsByNameNormalizedAsync(
            nameNormalized,
            excludingTagId: request.TagId,
            cancellationToken);

        if (nameExists)
        {
            return Result<UpdateTagResponseDto>.Failure(
                ContentErrors.Tag.NameNormalizedAlreadyExists);
        }

        try
        {
            tag.Update(
                name: request.Name,
                nameNormalized: nameNormalized,
                description: request.Description,
                isActive: request.IsActive,
                nowUtc: _dateTimeProvider.UtcNow,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Tag? updatedTag = await _tagRepository.UpdateAsync(
                tag,
                request.ExpectedVersion,
                cancellationToken);

            if (updatedTag is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<UpdateTagResponseDto>.Failure(
                    ContentErrors.ConcurrencyConflict);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<UpdateTagResponseDto>.Success(
                new UpdateTagResponseDto
                {
                    TagId = updatedTag.TagId,
                    Name = updatedTag.Name,
                    NameNormalized = updatedTag.NameNormalized,
                    Description = updatedTag.Description,
                    IsActive = updatedTag.IsActive,
                    Version = updatedTag.Version,
                    UpdatedAt = updatedTag.UpdatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<UpdateTagResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<UpdateTagResponseDto>.Failure(
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
            "CONTENT.TAG_CONFLICT" => ContentErrors.Tag.Conflict,
            _ => ContentErrors.WriteCommitFailed
        };
    }
}
