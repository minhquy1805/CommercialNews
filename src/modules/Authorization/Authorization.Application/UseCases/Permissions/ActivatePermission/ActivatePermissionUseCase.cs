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

namespace Authorization.Application.UseCases.Permissions.ActivatePermission;

public sealed class ActivatePermissionUseCase : IActivatePermissionUseCase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestContext _requestContext;
    private readonly IAuthorizationOutboxWriter _authorizationOutboxWriter;

    public ActivatePermissionUseCase(
        IPermissionRepository permissionRepository,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestContext requestContext,
        IAuthorizationOutboxWriter authorizationOutboxWriter)
    {
        _permissionRepository = permissionRepository
            ?? throw new ArgumentNullException(nameof(permissionRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
        _authorizationOutboxWriter = authorizationOutboxWriter
            ?? throw new ArgumentNullException(nameof(authorizationOutboxWriter));
    }

    public async Task<Result<ActivatePermissionResponseDto>> ExecuteAsync(
        ActivatePermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ActivatePermissionValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<ActivatePermissionResponseDto>.Failure(validationError);
        }

        try
        {
            var permission = await _permissionRepository.GetByIdAsync(
                request.PermissionId,
                cancellationToken);

            if (permission is null)
            {
                return Result<ActivatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.NotFound);
            }

            if (permission.IsActive)
            {
                return Result<ActivatePermissionResponseDto>.Success(
                    new ActivatePermissionResponseDto
                    {
                        PermissionId = permission.PermissionId,
                        IsActivated = true,
                        WasAlreadyActivated = true
                    });
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var actorUserId = _requestContext.CurrentUserId;
            var correlationId = _requestContext.CorrelationId;

            permission.Activate(
                nowUtc,
                actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var updatedPermission = await _permissionRepository.UpdateAsync(
                    permission,
                    cancellationToken);

                await _authorizationOutboxWriter.EnqueuePermissionActivatedAsync(
                    unitOfWork: _unitOfWork,
                    permissionId: updatedPermission.PermissionId,
                    permissionPublicId: updatedPermission.PublicId,
                    permissionKey: updatedPermission.Key,
                    permissionModule: updatedPermission.Module,
                    permissionAction: updatedPermission.Action,
                    permissionIsSystem: updatedPermission.IsSystem,
                    activatedByUserId: updatedPermission.UpdatedByUserId,
                    activatedAtUtc: updatedPermission.UpdatedAt,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<ActivatePermissionResponseDto>.Success(
                    new ActivatePermissionResponseDto
                    {
                        PermissionId = updatedPermission.PermissionId,
                        IsActivated = true,
                        WasAlreadyActivated = false
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
            return Result<ActivatePermissionResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuthorizationDomainException exception)
        {
            return Result<ActivatePermissionResponseDto>.Failure(
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