using Authorization.Application.Contracts.UserRoles;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Application.Validation.UserRoles;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.UserRoles.RevokeRoleFromUser;

public sealed class RevokeRoleFromUserUseCase : IRevokeRoleFromUserUseCase
{
    private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;

    public RevokeRoleFromUserUseCase(
        IAuthorizationUserLookupService authorizationUserLookupService,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IAuthorizationUnitOfWork unitOfWork)
    {
        _authorizationUserLookupService = authorizationUserLookupService;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RevokeRoleFromUserResponseDto>> ExecuteAsync(
        RevokeRoleFromUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = RevokeRoleFromUserValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<RevokeRoleFromUserResponseDto>.Failure(validationError);
        }

        try
        {
            var userExists = await _authorizationUserLookupService.ExistsAsync(
                request.UserId,
                cancellationToken);

            if (!userExists)
            {
                return Result<RevokeRoleFromUserResponseDto>.Failure(
                    AuthorizationErrors.User.NotFound);
            }

            var role = await _roleRepository.GetByIdAsync(
                request.RoleId,
                cancellationToken);

            if (role is null)
            {
                return Result<RevokeRoleFromUserResponseDto>.Failure(
                    AuthorizationErrors.Role.NotFound);
            }

            var existingAssignment = await _userRoleRepository.GetByUserIdAndRoleIdAsync(
                request.UserId,
                request.RoleId,
                cancellationToken);

            if (existingAssignment is null)
            {
                return Result<RevokeRoleFromUserResponseDto>.Success(
                    new RevokeRoleFromUserResponseDto
                    {
                        UserId = request.UserId,
                        RoleId = request.RoleId,
                        IsRevoked = true,
                        WasAlreadyRevoked = true
                    });
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var revoked = await _userRoleRepository.RevokeAsync(
                    request.UserId,
                    request.RoleId,
                    cancellationToken);

                if (!revoked)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);

                    return Result<RevokeRoleFromUserResponseDto>.Success(
                        new RevokeRoleFromUserResponseDto
                        {
                            UserId = request.UserId,
                            RoleId = request.RoleId,
                            IsRevoked = true,
                            WasAlreadyRevoked = true
                        });
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<RevokeRoleFromUserResponseDto>.Success(
                    new RevokeRoleFromUserResponseDto
                    {
                        UserId = request.UserId,
                        RoleId = request.RoleId,
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
            return Result<RevokeRoleFromUserResponseDto>.Failure(
                MapPersistenceException(exception));
        }
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "AUTHORIZATION.USER_NOT_FOUND" =>
                AuthorizationErrors.User.NotFound,

            "AUTHORIZATION.ROLE_NOT_FOUND" =>
                AuthorizationErrors.Role.NotFound,

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}