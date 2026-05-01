using Authorization.Application.Common;
using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Application.Validation.Permissions;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.Permissions.UpdatePermission;

public sealed class UpdatePermissionUseCase : IUpdatePermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRequestContext _requestContext;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthorizationOutboxWriter _authorizationOutboxWriter;

    public UpdatePermissionUseCase(
        IPermissionRepository permissionRepository,
        IRequestContext requestContext,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IAuthorizationOutboxWriter authorizationOutboxWriter)
    {
        _permissionRepository = permissionRepository
            ?? throw new ArgumentNullException(nameof(permissionRepository));
        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _authorizationOutboxWriter = authorizationOutboxWriter
            ?? throw new ArgumentNullException(nameof(authorizationOutboxWriter));
    }

    public async Task<Result<UpdatePermissionResponseDto>> ExecuteAsync(
        UpdatePermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = UpdatePermissionValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<UpdatePermissionResponseDto>.Failure(validationError);
        }

        try
        {
            var permission = await _permissionRepository.GetByIdAsync(
                request.PermissionId,
                cancellationToken);

            if (permission is null)
            {
                return Result<UpdatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.NotFound);
            }

            var normalizedKey = AuthorizationNameNormalizer.Normalize(request.Key);

            var existingPermission = await _permissionRepository.GetByKeyNormalizedAsync(
                normalizedKey,
                cancellationToken);

            if (existingPermission is not null &&
                existingPermission.PermissionId != permission.PermissionId)
            {
                return Result<UpdatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.Exists);
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var actorUserId = _requestContext.CurrentUserId;
            var correlationId = _requestContext.CorrelationId;

            permission.UpdateMetadata(
                key: request.Key.Trim(),
                keyNormalized: normalizedKey,
                module: request.Module,
                action: request.Action,
                description: request.Description,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var updatedPermission = await _permissionRepository.UpdateAsync(
                    permission,
                    cancellationToken);

                await _authorizationOutboxWriter.EnqueuePermissionUpdatedAsync(
                    unitOfWork: _unitOfWork,
                    permissionId: updatedPermission.PermissionId,
                    permissionPublicId: updatedPermission.PublicId,
                    permissionKey: updatedPermission.Key,
                    permissionKeyNormalized: updatedPermission.KeyNormalized,
                    permissionModule: updatedPermission.Module,
                    permissionAction: updatedPermission.Action,
                    permissionDescription: updatedPermission.Description,
                    permissionIsSystem: updatedPermission.IsSystem,
                    permissionIsActive: updatedPermission.IsActive,
                    updatedByUserId: updatedPermission.UpdatedByUserId,
                    updatedAtUtc: updatedPermission.UpdatedAt,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UpdatePermissionResponseDto>.Success(
                    new UpdatePermissionResponseDto
                    {
                        PermissionId = updatedPermission.PermissionId,
                        PublicId = updatedPermission.PublicId,
                        Key = updatedPermission.Key,
                        KeyNormalized = updatedPermission.KeyNormalized,
                        Module = updatedPermission.Module,
                        Action = updatedPermission.Action,
                        Description = updatedPermission.Description,
                        IsSystem = updatedPermission.IsSystem,
                        IsActive = updatedPermission.IsActive,
                        UpdatedAt = updatedPermission.UpdatedAt,
                        UpdatedByUserId = updatedPermission.UpdatedByUserId
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
            return Result<UpdatePermissionResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuthorizationDomainException exception)
        {
            return Result<UpdatePermissionResponseDto>.Failure(
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