using Authorization.Application.Contracts.UserRoles;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Application.Validation.UserRoles;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.UseCases.UserRoles.GetUserRoles;

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
        var validationError = GetUserRolesValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<GetUserRolesResponseDto>.Failure(validationError);
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

            var roles = await _userRoleRepository.GetRolesByUserIdAsync(
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
                        DisplayName = x.DisplayName,
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

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}