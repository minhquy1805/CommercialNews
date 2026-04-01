using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.GetUserRoles
{
    public sealed class GetUserRolesUseCase : IGetUserRolesUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IUserRoleRepository _userRoleRepository;

        public GetUserRolesUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IUserRoleRepository userRoleRepository)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<Result<GetUserRolesResponseDto>> ExecuteAsync(
            GetUserRolesRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UserId <= 0)
            {
                return Result<GetUserRolesResponseDto>.Failure(
                    AuthorizationErrors.UserRole.InvalidUserId);
            }

            try
            {
                var userExists = await _authorizationUserLookupService.ExistsAsync(
                    request.UserId,
                    cancellationToken);

                if (!userExists)
                {
                    return Result<GetUserRolesResponseDto>.Failure(
                        AuthorizationErrors.User.NotFound);
                }

                var roles = await _userRoleRepository.GetActiveRolesByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                return Result<GetUserRolesResponseDto>.Success(
                    new GetUserRolesResponseDto
                    {
                        UserId = request.UserId,
                        Roles = roles.Select(x => new UserRoleItemDto
                        {
                            RoleId = x.RoleId,
                            PublicId = x.PublicId,
                            Name = x.Name,
                            NameNormalized = x.NameNormalized,
                            Description = x.Description,
                            IsSystem = x.IsSystem,
                            IsActive = x.IsActive,
                            AssignedAt = x.AssignedAt,
                            AssignedByUserId = x.AssignedByUserId
                        }).ToList()
                    });
            }
            catch (PersistenceException exception)
            {
                return Result<GetUserRolesResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.USER_NOT_FOUND" =>
                    AuthorizationErrors.User.NotFound,

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}