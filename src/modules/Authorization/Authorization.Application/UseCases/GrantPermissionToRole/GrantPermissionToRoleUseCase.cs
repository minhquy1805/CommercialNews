using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.GrantPermissionToRole
{
    public sealed class GrantPermissionToRoleUseCase : IGrantPermissionToRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public GrantPermissionToRoleUseCase(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository,
            IAuthorizationUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IRequestContext requestContext)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
        }

        public async Task<Result<GrantPermissionToRoleResponseDto>> ExecuteAsync(
            GrantPermissionToRoleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                return Result<GrantPermissionToRoleResponseDto>.Failure(
                    AuthorizationErrors.RolePermission.InvalidRoleId);
            }

            if (request.PermissionId <= 0)
            {
                return Result<GrantPermissionToRoleResponseDto>.Failure(
                    AuthorizationErrors.RolePermission.InvalidPermissionId);
            }

            try
            {
                var role = await _roleRepository.GetByIdAsync(
                    request.RoleId,
                    cancellationToken);

                if (role is null)
                {
                    return Result<GrantPermissionToRoleResponseDto>.Failure(
                        AuthorizationErrors.Role.NotFound);
                }

                if (!role.IsActive)
                {
                    return Result<GrantPermissionToRoleResponseDto>.Failure(
                        AuthorizationErrors.Role.Inactive);
                }

                var permission = await _permissionRepository.GetByIdAsync(
                    request.PermissionId,
                    cancellationToken);

                if (permission is null)
                {
                    return Result<GrantPermissionToRoleResponseDto>.Failure(
                        AuthorizationErrors.Permission.NotFound);
                }

                if (!permission.IsActive)
                {
                    return Result<GrantPermissionToRoleResponseDto>.Failure(
                        AuthorizationErrors.Permission.Inactive);
                }

                var existingGrant = await _rolePermissionRepository.GetActiveByRoleIdAndPermissionIdAsync(
                    request.RoleId,
                    request.PermissionId,
                    cancellationToken);

                if (existingGrant is not null)
                {
                    return Result<GrantPermissionToRoleResponseDto>.Success(
                        new GrantPermissionToRoleResponseDto
                        {
                            RolePermissionId = existingGrant.RolePermissionId,
                            RoleId = existingGrant.RoleId,
                            PermissionId = existingGrant.PermissionId,
                            IsGranted = true,
                            WasAlreadyGranted = true
                        });
                }

                var nowUtc = _dateTimeProvider.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                var newGrant = RolePermission.CreateNew(
                    roleId: request.RoleId,
                    permissionId: request.PermissionId,
                    grantedAt: nowUtc,
                    grantedByUserId: actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var createdGrant = await _rolePermissionRepository.InsertAsync(
                        newGrant,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<GrantPermissionToRoleResponseDto>.Success(
                        new GrantPermissionToRoleResponseDto
                        {
                            RolePermissionId = createdGrant.RolePermissionId,
                            RoleId = createdGrant.RoleId,
                            PermissionId = createdGrant.PermissionId,
                            IsGranted = true,
                            WasAlreadyGranted = false
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
                return Result<GrantPermissionToRoleResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (AuthorizationDomainException exception)
            {
                return Result<GrantPermissionToRoleResponseDto>.Failure(
                    MapDomainException(exception));
            }
        }

        private static Error MapDomainException(AuthorizationDomainException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_PERMISSION_ID" =>
                    AuthorizationErrors.RolePermission.InvalidRolePermissionId,

                "AUTHORIZATION.ROLE_PERMISSION_INVALID_ROLE_ID" =>
                    AuthorizationErrors.RolePermission.InvalidRoleId,

                "AUTHORIZATION.ROLE_PERMISSION_INVALID_PERMISSION_ID" =>
                    AuthorizationErrors.RolePermission.InvalidPermissionId,

                "AUTHORIZATION.ROLE_PERMISSION_INVALID_REVOKE_TIME" =>
                    AuthorizationErrors.RolePermission.InvalidRevokeTime,

                "AUTHORIZATION.ROLE_PERMISSION_INVALID_REVOKE_STATE" =>
                    AuthorizationErrors.RolePermission.InvalidRevokeState,

                "AUTHORIZATION.ROLE_PERMISSION_ALREADY_REVOKED" =>
                    AuthorizationErrors.RolePermission.AlreadyRevoked,

                _ => AuthorizationErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.ROLE_NOT_FOUND" =>
                    AuthorizationErrors.Role.NotFound,

                "AUTHORIZATION.PERMISSION_NOT_FOUND" =>
                    AuthorizationErrors.Permission.NotFound,

                "AUTHORIZATION.ROLE_PERMISSION_ALREADY_GRANTED" =>
                    AuthorizationErrors.RolePermission.AlreadyGranted,

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}