using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
namespace Authorization.Application.UseCases.AssignRoleToUser
{
    public sealed class AssignRoleToUserUseCase : IAssignRoleToUserUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public AssignRoleToUserUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            IAuthorizationUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IRequestContext requestContext)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
        }

        public async Task<Result<AssignRoleToUserResponseDto>> ExecuteAsync(
            AssignRoleToUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UserId <= 0)
            {
                return Result<AssignRoleToUserResponseDto>.Failure(
                    AuthorizationErrors.UserRole.InvalidUserId);
            }

            if (request.RoleId <= 0)
            {
                return Result<AssignRoleToUserResponseDto>.Failure(
                    AuthorizationErrors.UserRole.InvalidRoleId);
            }

            try
            {
                var userExists = await _authorizationUserLookupService.ExistsAsync(
                    request.UserId,
                    cancellationToken);

                if (!userExists)
                {
                    return Result<AssignRoleToUserResponseDto>.Failure(
                        AuthorizationErrors.User.NotFound);
                }

                var role = await _roleRepository.GetByIdAsync(
                    request.RoleId,
                    cancellationToken);

                if (role is null)
                {
                    return Result<AssignRoleToUserResponseDto>.Failure(
                        AuthorizationErrors.Role.NotFound);
                }

                if (!role.IsActive)
                {
                    return Result<AssignRoleToUserResponseDto>.Failure(
                        AuthorizationErrors.Role.Inactive);
                }

                var existingAssignment = await _userRoleRepository.GetActiveByUserIdAndRoleIdAsync(
                    request.UserId,
                    request.RoleId,
                    cancellationToken);

                if (existingAssignment is not null)
                {
                    return Result<AssignRoleToUserResponseDto>.Success(
                        new AssignRoleToUserResponseDto
                        {
                            UserRoleId = existingAssignment.UserRoleId,
                            UserId = existingAssignment.UserId,
                            RoleId = existingAssignment.RoleId,
                            IsAssigned = true,
                            WasAlreadyAssigned = true
                        });
                }

                var nowUtc = _dateTimeProvider.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                var newAssignment = UserRole.CreateNew(
                    userId: request.UserId,
                    roleId: request.RoleId,
                    assignedAt: nowUtc,
                    assignedByUserId: actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var createdAssignment = await _userRoleRepository.InsertAsync(
                        newAssignment,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<AssignRoleToUserResponseDto>.Success(
                        new AssignRoleToUserResponseDto
                        {
                            UserRoleId = createdAssignment.UserRoleId,
                            UserId = createdAssignment.UserId,
                            RoleId = createdAssignment.RoleId,
                            IsAssigned = true,
                            WasAlreadyAssigned = false
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
                return Result<AssignRoleToUserResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (AuthorizationDomainException exception)
            {
                return Result<AssignRoleToUserResponseDto>.Failure(
                    MapDomainException(exception));
            }
        }

        private static Error MapDomainException(AuthorizationDomainException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.USER_ROLE_INVALID_USER_ROLE_ID" =>
                    AuthorizationErrors.UserRole.InvalidUserRoleId,

                "AUTHORIZATION.USER_ROLE_INVALID_USER_ID" =>
                    AuthorizationErrors.UserRole.InvalidUserId,

                "AUTHORIZATION.USER_ROLE_INVALID_ROLE_ID" =>
                    AuthorizationErrors.UserRole.InvalidRoleId,

                "AUTHORIZATION.USER_ROLE_INVALID_REVOKE_TIME" =>
                    AuthorizationErrors.UserRole.InvalidRevokeTime,

                "AUTHORIZATION.USER_ROLE_INVALID_REVOKE_STATE" =>
                    AuthorizationErrors.UserRole.InvalidRevokeState,

                "AUTHORIZATION.USER_ROLE_ALREADY_REVOKED" =>
                    AuthorizationErrors.UserRole.AlreadyRevoked,

                _ => AuthorizationErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.USER_NOT_FOUND" =>
                    AuthorizationErrors.User.NotFound,

                "AUTHORIZATION.ROLE_NOT_FOUND" =>
                    AuthorizationErrors.Role.NotFound,

                "AUTHORIZATION.USER_ROLE_ALREADY_ASSIGNED" =>
                    AuthorizationErrors.UserRole.AlreadyAssigned,

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}