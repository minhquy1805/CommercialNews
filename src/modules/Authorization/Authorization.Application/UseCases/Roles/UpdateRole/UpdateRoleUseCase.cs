using Authorization.Application.Common;
using Authorization.Application.Contracts.Outbox.Payload;
using Authorization.Application.Contracts.Roles;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Application.Validation.Roles;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.Roles.UpdateRole;

public sealed class UpdateRoleUseCase : IUpdateRoleUseCase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRequestContext _requestContext;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthorizationOutboxWriter _authorizationOutboxWriter;

    public UpdateRoleUseCase(
        IRoleRepository roleRepository,
        IRequestContext requestContext,
        IAuthorizationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IAuthorizationOutboxWriter authorizationOutboxWriter)
    {
        _roleRepository = roleRepository
            ?? throw new ArgumentNullException(nameof(roleRepository));
        _requestContext = requestContext
            ?? throw new ArgumentNullException(nameof(requestContext));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _authorizationOutboxWriter = authorizationOutboxWriter
            ?? throw new ArgumentNullException(nameof(authorizationOutboxWriter));
    }

    public async Task<Result<UpdateRoleResponseDto>> ExecuteAsync(
        UpdateRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = UpdateRoleValidator.Validate(request);
        if (validationError is not null)
        {
            return Result<UpdateRoleResponseDto>.Failure(validationError);
        }

        try
        {
            var role = await _roleRepository.GetByIdAsync(
                request.RoleId,
                cancellationToken);

            if (role is null)
            {
                return Result<UpdateRoleResponseDto>.Failure(
                    AuthorizationErrors.Role.NotFound);
            }

            var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

            var existingRole = await _roleRepository.GetByNameNormalizedAsync(
                normalizedName,
                cancellationToken);

            if (existingRole is not null &&
                existingRole.RoleId != role.RoleId)
            {
                return Result<UpdateRoleResponseDto>.Failure(
                    AuthorizationErrors.Role.Exists);
            }

            var nowUtc = _dateTimeProvider.UtcNow;
            var actorUserId = _requestContext.CurrentUserId;

            role.UpdateMetadata(
                name: request.Name.Trim(),
                nameNormalized: normalizedName,
                displayName: request.DisplayName,
                description: request.Description,
                nowUtc: nowUtc,
                actorUserId: actorUserId);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var updatedRole = await _roleRepository.UpdateAsync(
                    role,
                    cancellationToken);

                await _authorizationOutboxWriter.EnqueueRoleUpdatedAsync(
                    new RoleUpdatedOutboxPayload
                    {
                        RoleId = updatedRole.RoleId,
                        RolePublicId = updatedRole.PublicId,
                        RoleName = updatedRole.Name,
                        RoleNameNormalized = updatedRole.NameNormalized,
                        DisplayName = updatedRole.DisplayName,
                        Description = updatedRole.Description,
                        IsSystem = updatedRole.IsSystem,
                        IsActive = updatedRole.IsActive,
                        OccurredAtUtc = nowUtc,
                        ActorUserId = actorUserId,
                        CorrelationId = _requestContext.CorrelationId
                    },
                    cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<UpdateRoleResponseDto>.Success(
                    new UpdateRoleResponseDto
                    {
                        RoleId = updatedRole.RoleId,
                        PublicId = updatedRole.PublicId,
                        Name = updatedRole.Name,
                        NameNormalized = updatedRole.NameNormalized,
                        DisplayName = updatedRole.DisplayName,
                        Description = updatedRole.Description,
                        IsSystem = updatedRole.IsSystem,
                        IsActive = updatedRole.IsActive,
                        UpdatedAt = updatedRole.UpdatedAt,
                        UpdatedByUserId = updatedRole.UpdatedByUserId
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
            return Result<UpdateRoleResponseDto>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuthorizationDomainException exception)
        {
            return Result<UpdateRoleResponseDto>.Failure(
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