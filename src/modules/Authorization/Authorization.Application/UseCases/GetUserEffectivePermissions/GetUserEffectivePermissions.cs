using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.GetUserEffectivePermissions
{
    public sealed class GetUserEffectivePermissionsUseCase : IGetUserEffectivePermissionsUseCase
    {
        private readonly IAuthorizationUserLookupService _authorizationUserLookupService;
        private readonly IAuthorizationPermissionQueryRepository _authorizationPermissionQueryRepository;

        public GetUserEffectivePermissionsUseCase(
            IAuthorizationUserLookupService authorizationUserLookupService,
            IAuthorizationPermissionQueryRepository authorizationPermissionQueryRepository)
        {
            _authorizationUserLookupService = authorizationUserLookupService;
            _authorizationPermissionQueryRepository = authorizationPermissionQueryRepository;
        }

        public async Task<Result<GetUserEffectivePermissionsResponseDto>> ExecuteAsync(
            GetUserEffectivePermissionsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.UserId <= 0)
            {
                return Result<GetUserEffectivePermissionsResponseDto>.Failure(
                    AuthorizationErrors.UserRole.InvalidUserId);
            }

            try
            {
                var userExists = await _authorizationUserLookupService.ExistsAsync(
                    request.UserId,
                    cancellationToken);

                if (!userExists)
                {
                    return Result<GetUserEffectivePermissionsResponseDto>.Failure(
                        AuthorizationErrors.User.NotFound);
                }

                var permissions = await _authorizationPermissionQueryRepository.GetEffectivePermissionsByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                return Result<GetUserEffectivePermissionsResponseDto>.Success(
                    new GetUserEffectivePermissionsResponseDto
                    {
                        UserId = request.UserId,
                        Permissions = permissions.Select(x => new EffectivePermissionItemDto
                        {
                            PermissionId = x.PermissionId,
                            PublicId = x.PublicId,
                            Name = x.Name,
                            NameNormalized = x.NameNormalized,
                            Description = x.Description,
                            Module = x.Module,
                            IsSystem = x.IsSystem,
                            IsActive = x.IsActive
                        }).ToList()
                    });
            }
            catch (PersistenceException exception)
            {
                return Result<GetUserEffectivePermissionsResponseDto>.Failure(
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