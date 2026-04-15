using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.RevokeRoleFromUser
{
    public sealed class RevokeRoleFromUserUseCase : IRevokeRoleFromUserUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IAuthorizationUnitOfWork _unitOfWork;

        public RevokeRoleFromUserUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            IRequestContext requestContext,
            IAuthorizationUnitOfWork unitOfWork)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RevokeRoleFromUserResponseDto>> ExecuteAsync(
            RevokeRoleFromUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UserId <= 0)
            {
                return Result<RevokeRoleFromUserResponseDto>.Failure(
                    AuthorizationErrors.UserRole.InvalidUserId);
            }

            if (request.RoleId <= 0)
            {
                return Result<RevokeRoleFromUserResponseDto>.Failure(
                    AuthorizationErrors.UserRole.InvalidRoleId);
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

                var existingAssignment = await _userRoleRepository.GetActiveByUserIdAndRoleIdAsync(
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

                var actorUserId = _requestContext.CurrentUserId;

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var revoked = await _userRoleRepository.RevokeAsync(
                        request.UserId,
                        request.RoleId,
                        actorUserId,
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

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}