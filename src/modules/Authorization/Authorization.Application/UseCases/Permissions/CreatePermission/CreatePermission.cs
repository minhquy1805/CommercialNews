using Authorization.Application.Common;
using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Validation.Permissions;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.Permissions.CreatePermission;

public sealed class CreatePermissionUseCase : ICreatePermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePermissionUseCase(
        IPermissionRepository permissionRepository,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _permissionRepository = permissionRepository;
        _requestContext = requestContext;
        _publicIdGenerator = publicIdGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreatePermissionResponseDto>> ExecuteAsync(
        CreatePermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = CreatePermissionValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<CreatePermissionResponseDto>.Failure(validationError);
        }

        try
        {
            var normalizedKey = AuthorizationNameNormalizer.Normalize(request.Key);

            var existingPermission = await _permissionRepository.GetByKeyNormalizedAsync(
                normalizedKey,
                cancellationToken);

            if (existingPermission is not null)
            {
                return Result<CreatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.Exists);
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var actorUserId = _requestContext.CurrentUserId;

            var permission = Permission.CreateNew(
                publicId: _publicIdGenerator.NewId(),
                key: request.Key.Trim(),
                keyNormalized: normalizedKey,
                module: request.Module,
                action: request.Action,
                description: request.Description,
                isSystem: request.IsSystem,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var createdPermission = await _permissionRepository.InsertAsync(
                    permission,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<CreatePermissionResponseDto>.Success(
                    new CreatePermissionResponseDto
                    {
                        PermissionId = createdPermission.PermissionId,
                        PublicId = createdPermission.PublicId,
                        Key = createdPermission.Key,
                        KeyNormalized = createdPermission.KeyNormalized,
                        Module = createdPermission.Module,
                        Action = createdPermission.Action,
                        Description = createdPermission.Description,
                        IsSystem = createdPermission.IsSystem,
                        IsActive = createdPermission.IsActive,
                        CreatedAt = createdPermission.CreatedAt,
                        CreatedByUserId = createdPermission.CreatedByUserId
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
            return Result<CreatePermissionResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuthorizationDomainException exception)
        {
            return Result<CreatePermissionResponseDto>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(AuthorizationDomainException exception)
    {
        return exception.Code switch
        {
            "AUTHORIZATION.PERMISSION_PUBLIC_ID_REQUIRED" =>
                AuthorizationErrors.Permission.PublicIdRequired,

            "AUTHORIZATION.PERMISSION_KEY_REQUIRED" =>
                AuthorizationErrors.Permission.KeyRequired,

            "AUTHORIZATION.PERMISSION_KEY_TOO_LONG" =>
                AuthorizationErrors.Permission.KeyTooLong,

            "AUTHORIZATION.PERMISSION_KEY_NORMALIZED_REQUIRED" =>
                AuthorizationErrors.Permission.KeyNormalizedRequired,

            "AUTHORIZATION.PERMISSION_KEY_NORMALIZED_TOO_LONG" =>
                AuthorizationErrors.Permission.KeyNormalizedTooLong,

            "AUTHORIZATION.PERMISSION_MODULE_TOO_LONG" =>
                AuthorizationErrors.Permission.ModuleTooLong,

            "AUTHORIZATION.PERMISSION_ACTION_TOO_LONG" =>
                AuthorizationErrors.Permission.ActionTooLong,

            "AUTHORIZATION.PERMISSION_INVALID_PERMISSION_ID" =>
                AuthorizationErrors.Permission.InvalidPermissionId,

            "AUTHORIZATION.PERMISSION_INVALID_TIMESTAMP" =>
                AuthorizationErrors.Permission.InvalidTimestamp,

            "AUTHORIZATION.PERMISSION_STALE_UPDATE_TIME" =>
                AuthorizationErrors.Permission.StaleUpdateTime,

            "AUTHORIZATION.SYSTEM_PERMISSION_PROTECTED" =>
                AuthorizationErrors.Permission.SystemProtected,

            _ => AuthorizationErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "AUTHORIZATION.PERMISSION_EXISTS" =>
                AuthorizationErrors.Permission.Exists,

            "AUTHORIZATION.PERMISSION_NOT_FOUND" =>
                AuthorizationErrors.Permission.NotFound,

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}