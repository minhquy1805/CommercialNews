using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.RevokePermissionFromRole
{
    public sealed class RevokePermissionFromRoleUseCase : IRevokePermissionFromRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IAuthorizationUnitOfWork _unitOfWork;

        public RevokePermissionFromRoleUseCase(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository,
            IRequestContext requestContext,
            IAuthorizationUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RevokePermissionFromRoleResponseDto>> ExecuteAsync(
            RevokePermissionFromRoleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                return Result<RevokePermissionFromRoleResponseDto>.Failure(
                    AuthorizationErrors.RolePermission.InvalidRoleId);
            }

            if (request.PermissionId <= 0)
            {
                return Result<RevokePermissionFromRoleResponseDto>.Failure(
                    AuthorizationErrors.RolePermission.InvalidPermissionId);
            }

            try
            {
                var role = await _roleRepository.GetByIdAsync(
                    request.RoleId,
                    cancellationToken);

                if (role is null)
                {
                    return Result<RevokePermissionFromRoleResponseDto>.Failure(
                        AuthorizationErrors.Role.NotFound);
                }

                var permission = await _permissionRepository.GetByIdAsync(
                    request.PermissionId,
                    cancellationToken);

                if (permission is null)
                {
                    return Result<RevokePermissionFromRoleResponseDto>.Failure(
                        AuthorizationErrors.Permission.NotFound);
                }

                var existingGrant = await _rolePermissionRepository.GetActiveByRoleIdAndPermissionIdAsync(
                    request.RoleId,
                    request.PermissionId,
                    cancellationToken);

                if (existingGrant is null)
                {
                    return Result<RevokePermissionFromRoleResponseDto>.Success(
                        new RevokePermissionFromRoleResponseDto
                        {
                            RoleId = request.RoleId,
                            PermissionId = request.PermissionId,
                            IsRevoked = true,
                            WasAlreadyRevoked = true
                        });
                }

                var actorUserId = _requestContext.CurrentUserId;

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var revoked = await _rolePermissionRepository.RevokeAsync(
                        request.RoleId,
                        request.PermissionId,
                        actorUserId,
                        cancellationToken);

                    if (!revoked)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);

                        return Result<RevokePermissionFromRoleResponseDto>.Success(
                            new RevokePermissionFromRoleResponseDto
                            {
                                RoleId = request.RoleId,
                                PermissionId = request.PermissionId,
                                IsRevoked = true,
                                WasAlreadyRevoked = true
                            });
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<RevokePermissionFromRoleResponseDto>.Success(
                        new RevokePermissionFromRoleResponseDto
                        {
                            RoleId = request.RoleId,
                            PermissionId = request.PermissionId,
                            IsRevoked = true,
                            WasAlreadyRevoked = false
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
                return Result<RevokePermissionFromRoleResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.ROLE_NOT_FOUND" =>
                    AuthorizationErrors.Role.NotFound,

                "AUTHORIZATION.PERMISSION_NOT_FOUND" =>
                    AuthorizationErrors.Permission.NotFound,

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}