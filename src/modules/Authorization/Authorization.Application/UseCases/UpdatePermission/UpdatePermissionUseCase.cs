using Authorization.Application.Common;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.UpdatePermission
{
    public sealed class UpdatePermissionUseCase : IUpdatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdatePermissionUseCase(
            IPermissionRepository permissionRepository,
            IRequestContext requestContext,
            IAuthorizationUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _permissionRepository = permissionRepository;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<UpdatePermissionResponseDto>> ExecuteAsync(
            UpdatePermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.PermissionId <= 0)
            {
                return Result<UpdatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.InvalidPermissionId);
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<UpdatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.NameRequired);
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

                var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

                var existingPermission = await _permissionRepository.GetByNameNormalizedAsync(
                    normalizedName,
                    cancellationToken);

                if (existingPermission is not null &&
                    existingPermission.PermissionId != permission.PermissionId)
                {
                    return Result<UpdatePermissionResponseDto>.Failure(
                        AuthorizationErrors.Permission.Exists);
                }

                var nowUtc = _dateTimeProvider.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                permission.UpdateMetadata(
                    name: request.Name.Trim(),
                    nameNormalized: normalizedName,
                    description: request.Description,
                    module: request.Module,
                    nowUtc: nowUtc,
                    actorUserId: actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var updatedPermission = await _permissionRepository.UpdateAsync(
                        permission,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<UpdatePermissionResponseDto>.Success(
                        new UpdatePermissionResponseDto
                        {
                            PermissionId = updatedPermission.PermissionId,
                            PublicId = updatedPermission.PublicId,
                            Name = updatedPermission.Name,
                            NameNormalized = updatedPermission.NameNormalized,
                            Description = updatedPermission.Description,
                            Module = updatedPermission.Module,
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

                "AUTHORIZATION.PERMISSION_NAME_REQUIRED" =>
                    AuthorizationErrors.Permission.NameRequired,

                "AUTHORIZATION.PERMISSION_NAME_TOO_LONG" =>
                    AuthorizationErrors.Permission.NameTooLong,

                "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_REQUIRED" =>
                    AuthorizationErrors.Permission.NameNormalizedRequired,

                "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_TOO_LONG" =>
                    AuthorizationErrors.Permission.NameNormalizedTooLong,

                "AUTHORIZATION.PERMISSION_MODULE_TOO_LONG" =>
                    AuthorizationErrors.Permission.ModuleTooLong,

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

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}