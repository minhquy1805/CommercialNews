using Authorization.Application.Contracts.RolePermissions;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Validation.RolePermissions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RolePermissions.RevokePermissionFromRole;

public sealed class RevokePermissionFromRoleUseCase : IRevokePermissionFromRoleUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;

    public RevokePermissionFromRoleUseCase(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IAuthorizationUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RevokePermissionFromRoleResponseDto>> ExecuteAsync(
        RevokePermissionFromRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = RevokePermissionFromRoleValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<RevokePermissionFromRoleResponseDto>.Failure(validationError);
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

            var existingGrant = await _rolePermissionRepository.GetByRoleIdAndPermissionIdAsync(
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

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var revoked = await _rolePermissionRepository.RevokeAsync(
                    request.RoleId,
                    request.PermissionId,
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

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}