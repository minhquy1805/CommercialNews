using Authorization.Application.Common;
using Authorization.Application.Contracts.Roles;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Validation.Roles;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.Roles.CreateRole;

public sealed class CreateRoleUseCase : ICreateRoleUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRequestContext _requestContext;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRoleUseCase(
        IRoleRepository roleRepository,
        IRequestContext requestContext,
        IPublicIdGenerator publicIdGenerator,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _roleRepository = roleRepository;
        _requestContext = requestContext;
        _publicIdGenerator = publicIdGenerator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CreateRoleResponseDto>> ExecuteAsync(
        CreateRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = CreateRoleValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<CreateRoleResponseDto>.Failure(validationError);
        }

        try
        {
            var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

            var existingRole = await _roleRepository.GetByNameNormalizedAsync(
                normalizedName,
                cancellationToken);

            if (existingRole is not null)
            {
                return Result<CreateRoleResponseDto>.Failure(
                    AuthorizationErrors.Role.Exists);
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var actorUserId = _requestContext.CurrentUserId;

            var role = Role.CreateNew(
                publicId: _publicIdGenerator.NewId(),
                name: request.Name.Trim(),
                nameNormalized: normalizedName,
                displayName: request.DisplayName,
                description: request.Description,
                isSystem: request.IsSystem,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var createdRole = await _roleRepository.InsertAsync(
                    role,
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<CreateRoleResponseDto>.Success(
                    new CreateRoleResponseDto
                    {
                        RoleId = createdRole.RoleId,
                        PublicId = createdRole.PublicId,
                        Name = createdRole.Name,
                        NameNormalized = createdRole.NameNormalized,
                        DisplayName = createdRole.DisplayName,
                        Description = createdRole.Description,
                        IsSystem = createdRole.IsSystem,
                        IsActive = createdRole.IsActive,
                        CreatedAt = createdRole.CreatedAt,
                        CreatedByUserId = createdRole.CreatedByUserId
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
            return Result<CreateRoleResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuthorizationDomainException exception)
        {
            return Result<CreateRoleResponseDto>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error MapDomainException(AuthorizationDomainException exception)
    {
        return exception.Code switch
        {
            "AUTHORIZATION.ROLE_PUBLIC_ID_REQUIRED" =>
                AuthorizationErrors.Role.PublicIdRequired,

            "AUTHORIZATION.ROLE_NAME_REQUIRED" =>
                AuthorizationErrors.Role.NameRequired,

            "AUTHORIZATION.ROLE_NAME_TOO_LONG" =>
                AuthorizationErrors.Role.NameTooLong,

            "AUTHORIZATION.ROLE_NAME_NORMALIZED_REQUIRED" =>
                AuthorizationErrors.Role.NameNormalizedRequired,

            "AUTHORIZATION.ROLE_NAME_NORMALIZED_TOO_LONG" =>
                AuthorizationErrors.Role.NameNormalizedTooLong,

            "AUTHORIZATION.ROLE_INVALID_ROLE_ID" =>
                AuthorizationErrors.Role.InvalidRoleId,

            "AUTHORIZATION.ROLE_INVALID_TIMESTAMP" =>
                AuthorizationErrors.Role.InvalidTimestamp,

            "AUTHORIZATION.ROLE_STALE_UPDATE_TIME" =>
                AuthorizationErrors.Role.StaleUpdateTime,

            "AUTHORIZATION.SYSTEM_ROLE_PROTECTED" =>
                AuthorizationErrors.Role.SystemProtected,

            _ => AuthorizationErrors.ValidationFailed
        };
    }

    private static Error MapPersistenceException(PersistenceException exception)
    {
        return exception.Code switch
        {
            "AUTHORIZATION.ROLE_EXISTS" =>
                AuthorizationErrors.Role.Exists,

            "AUTHORIZATION.ROLE_NOT_FOUND" =>
                AuthorizationErrors.Role.NotFound,

            _ => AuthorizationErrors.UnexpectedError
        };
    }
}