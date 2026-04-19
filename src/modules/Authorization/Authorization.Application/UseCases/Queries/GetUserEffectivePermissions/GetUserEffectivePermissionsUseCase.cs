using Authorization.Application.Contracts.Queries;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Application.Validation.Queries;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.Queries.GetUserEffectivePermissions;

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
        var validationError = GetUserEffectivePermissionsValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetUserEffectivePermissionsResponseDto>.Failure(validationError);
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
                        Key = x.Key,
                        KeyNormalized = x.KeyNormalized,
                        Description = x.Description,
                        Module = x.Module,
                        Action = x.Action,
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

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}