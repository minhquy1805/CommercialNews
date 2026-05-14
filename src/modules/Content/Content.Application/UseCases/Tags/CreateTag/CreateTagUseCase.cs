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

namespace Content.Application.UseCases.Tags.CreateTag;

public sealed class CreateTagUseCase : ICreateTagUseCase
{
    private readonly ITagRepository _tagRepository;
    private readonly IContentUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTagUseCase(
        ITagRepository tagRepository,
        IContentUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publicIdGenerator = publicIdGenerator ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task<Result<CreateTagResponseDto>> ExecuteAsync(
        CreateTagRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string nameNormalized = NormalizeName(request.Name);

            bool nameExists = await _tagRepository.ExistsByNameNormalizedAsync(
                nameNormalized,
                excludingTagId: null,
                cancellationToken);

            if (nameExists)
            {
                return Result<CreateTagResponseDto>.Failure(
                    ContentErrors.Tag.NameNormalizedAlreadyExists);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            string publicId = _publicIdGenerator.NewId();

            Tag tag = Tag.Create(
                publicId: publicId,
                name: request.Name,
                nameNormalized: nameNormalized,
                description: request.Description,
                isActive: request.IsActive,
                nowUtc: nowUtc,
                actorUserId: request.ActorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            Tag? createdTag = await _tagRepository.InsertAsync(
                tag,
                cancellationToken);

            if (createdTag is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<CreateTagResponseDto>.Failure(
                    ContentErrors.WriteCommitFailed);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<CreateTagResponseDto>.Success(
                new CreateTagResponseDto
                {
                    TagId = createdTag.TagId,
                    PublicId = createdTag.PublicId,
                    Name = createdTag.Name,
                    NameNormalized = createdTag.NameNormalized,
                    Description = createdTag.Description,
                    IsActive = createdTag.IsActive,
                    Version = createdTag.Version,
                    CreatedAt = createdTag.CreatedAt
                });
        }
        catch (PersistenceException exception)
        {
            await RollbackIfNeededAsync(cancellationToken);

            return Result<CreateTagResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (ContentDomainException exception)
        {
            return Result<CreateTagResponseDto>.Failure(
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
            "CONTENT.TAG_CONFLICT" => ContentErrors.Tag.Conflict,
            "CONTENT.CONCURRENCY_CONFLICT" => ContentErrors.ConcurrencyConflict,
            _ => ContentErrors.WriteCommitFailed
        };
    }
}
