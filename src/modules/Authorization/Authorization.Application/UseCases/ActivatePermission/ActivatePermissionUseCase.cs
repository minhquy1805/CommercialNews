using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.ActivatePermission
{
    public sealed class ActivatePermissionUseCase : IActivatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public ActivatePermissionUseCase(
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

        public async Task<Result<ActivatePermissionResponseDto>> ExecuteAsync(
            ActivatePermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PermissionId <= 0)
            {
                return Result<ActivatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.InvalidPermissionId);
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
                            PermissionId = request.PermissionId,
                            IsActivated = true,
                            WasAlreadyActivated = true
                        });
                }

                var nowUtc = _dateTimeProvider.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                permission.Activate(
                    nowUtc,
                    actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var updatedPermission = await _permissionRepository.UpdateAsync(
                        permission,
                        cancellationToken);

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

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}