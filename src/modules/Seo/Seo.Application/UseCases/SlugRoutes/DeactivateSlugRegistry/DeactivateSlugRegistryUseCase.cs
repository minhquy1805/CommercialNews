using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Seo.Application.Contracts.SlugRegistry.Requests;
using Seo.Application.Contracts.SlugRegistry.Responses;
using Seo.Application.Errors;
using Seo.Application.Ports.Persistence;
using Seo.Domain.Exceptions;

namespace Seo.Application.UseCases.SlugRoutes.DeactivateSlugRegistry;

public sealed class DeactivateSlugRegistryUseCase : IDeactivateSlugRegistryUseCase
{
    private readonly ISlugRegistryRepository _slugRegistryRepository;
    private readonly ISeoUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public DeactivateSlugRegistryUseCase(
        ISlugRegistryRepository slugRegistryRepository,
        ISeoUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _slugRegistryRepository = slugRegistryRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<DeactivateSlugRegistryResponse>> ExecuteAsync(
        DeactivateSlugRegistryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.SlugId <= 0)
            {
                return Result<DeactivateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.InvalidSlugId);
            }

            var existing = await _slugRegistryRepository.GetByIdAsync(
                request.SlugId,
                cancellationToken);

            if (existing is null)
            {
                return Result<DeactivateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.NotFound);
            }

            if (!existing.IsActive)
            {
                return Result<DeactivateSlugRegistryResponse>.Failure(
                    SeoErrors.SlugRegistry.AlreadyInactive);
            }

            DateTime nowUtc = _dateTimeProvider.UtcNow;
            long? actorUserId = request.UpdatedByUserId ?? _requestContext.CurrentUserId;

            existing.Deactivate(nowUtc, actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await _slugRegistryRepository.DeactivateAsync(
                    request.SlugId,
                    actorUserId,
                    request.ExpectedVersion,
                    cancellationToken);

                if (affectedRows <= 0)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<DeactivateSlugRegistryResponse>.Failure(
                        SeoErrors.SlugRegistry.VersionMismatch);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<DeactivateSlugRegistryResponse>.Success(
                    new DeactivateSlugRegistryResponse
                    {
                        SlugId = existing.SlugId,
                        ArticleId = existing.ArticleId,
                        Slug = existing.Slug,
                        Scope = existing.Scope,
                        IsActive = false,
                        IsIndexable = existing.IsIndexable,
                        Version = existing.Version,
                        UpdatedAt = existing.UpdatedAt,
                        UpdatedByUserId = existing.UpdatedByUserId
                    });
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<DeactivateSlugRegistryResponse>.Failure(
                MapPersistenceException(exception));
        }
        catch (SeoDomainException exception)
        {
            return Result<DeactivateSlugRegistryResponse>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(SeoDomainException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_REGISTRY_INVALID_SLUG_ID" => SeoErrors.SlugRegistry.InvalidSlugId,
            "SEO.SLUG_REGISTRY_ALREADY_INACTIVE" => SeoErrors.SlugRegistry.AlreadyInactive,
            "SEO.SLUG_REGISTRY_INVALID_VERSION" => SeoErrors.SlugRegistry.InvalidVersion,
            _ => SeoErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "SEO.SLUG_CONFLICT" => SeoErrors.SlugRegistry.Conflict,
            "SEO.VERSION_MISMATCH" => SeoErrors.SlugRegistry.VersionMismatch,
            "SEO.STALE_WRITE_CONFLICT" => SeoErrors.SlugRegistry.StaleWriteConflict,
            _ => SeoErrors.ValidationFailed
        };
    }
}