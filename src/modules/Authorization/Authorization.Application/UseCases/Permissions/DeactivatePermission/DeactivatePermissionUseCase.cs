using Authorization.Application.Contracts.Permissions;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Validation.Permissions;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.Permissions.DeactivatePermission;

public sealed class DeactivatePermissionUseCase : IDeactivatePermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;

    public DeactivatePermissionUseCase(
        IPermissionRepository permissionRepository,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _requestContext = requestContext;
    }

    public async Task<Result<DeactivatePermissionResponseDto>> ExecuteAsync(
        DeactivatePermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = DeactivatePermissionValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<DeactivatePermissionResponseDto>.Failure(validationError);
        }

        try
        {
            var permission = await _permissionRepository.GetByIdAsync(
                request.PermissionId,
                cancellationToken);

            if (permission is null)
            {
                return Result<DeactivatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.NotFound);
            }

            if (!permission.IsActive)
            {
                return Result<DeactivatePermissionResponseDto>.Success(
                    new DeactivatePermissionResponseDto
                    {
                        PermissionId = permission.PermissionId,
                        IsDeactivated = true,
                        WasAlreadyDeactivated = true
                    });
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var actorUserId = _requestContext.CurrentUserId;

            permission.Deactivate(
                nowUtc,
                actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var updatedPermission = await _permissionRepository.UpdateAsync(
                    permission,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<DeactivatePermissionResponseDto>.Success(
                    new DeactivatePermissionResponseDto
                    {
                        PermissionId = updatedPermission.PermissionId,
                        IsDeactivated = true,
                        WasAlreadyDeactivated = false
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
            return Result<DeactivatePermissionResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuthorizationDomainException exception)
        {
            return Result<DeactivatePermissionResponseDto>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(AuthorizationDomainException exception)
    {
        return exception.Code switch
        {
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
            "AUTHORIZATION.PERMISSION_NOT_FOUND" =>
                AuthorizationErrors.Permission.NotFound,

            "AUTHORIZATION.PERMISSION_EXISTS" =>
                AuthorizationErrors.Permission.Exists,

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}