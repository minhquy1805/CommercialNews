using Authorization.Application.Consumers.Identity.Payloads;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Application.Ports.Services;
using Authorization.Domain.Constants;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace Authorization.Application.Consumers.Identity;

public sealed class IdentityUserRegisteredConsumerService
    : IIdentityUserRegisteredConsumerService
{
    private readonly IAuthorizationUserLookupService _userLookupService;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IAuthorizationOutboxWriter _outboxWriter;

    public IdentityUserRegisteredConsumerService(
        IAuthorizationUserLookupService userLookupService,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IAuthorizationUnitOfWork unitOfWork,
        IAuthorizationOutboxWriter outboxWriter)
    {
        _userLookupService = userLookupService
            ?? throw new ArgumentNullException(nameof(userLookupService));
        _roleRepository = roleRepository
            ?? throw new ArgumentNullException(nameof(roleRepository));
        _userRoleRepository = userRoleRepository
            ?? throw new ArgumentNullException(nameof(userRoleRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _outboxWriter = outboxWriter
            ?? throw new ArgumentNullException(nameof(outboxWriter));
    }

    public async Task<Result<IdentityUserRegisteredRoleAssignmentResult>> AssignDefaultRoleAsync(
        string messageId,
        string? correlationId,
        IdentityUserRegisteredPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        Error? validationError = ValidatePayload(messageId, payload);
        if (validationError is not null)
        {
            return Result<IdentityUserRegisteredRoleAssignmentResult>.Failure(
                validationError);
        }

        try
        {
            bool userExists = await _userLookupService.ExistsAsync(
                payload.UserId,
                cancellationToken);

            if (!userExists)
            {
                return Result<IdentityUserRegisteredRoleAssignmentResult>.Failure(
                    AuthorizationErrors.User.NotFound);
            }

            Role? defaultRole = await _roleRepository.GetByNameNormalizedAsync(
                NormalizeRoleName(SystemRoles.User),
                cancellationToken);

            if (defaultRole is null)
            {
                return Result<IdentityUserRegisteredRoleAssignmentResult>.Failure(
                    AuthorizationErrors.Role.NotFound);
            }

            if (!defaultRole.IsActive)
            {
                return Result<IdentityUserRegisteredRoleAssignmentResult>.Failure(
                    AuthorizationErrors.Role.Inactive);
            }

            UserRole? existingAssignment =
                await _userRoleRepository.GetByUserIdAndRoleIdAsync(
                    payload.UserId,
                    defaultRole.RoleId,
                    cancellationToken);

            if (existingAssignment is not null)
            {
                return Result<IdentityUserRegisteredRoleAssignmentResult>.Success(
                    new IdentityUserRegisteredRoleAssignmentResult(
                        UserId: existingAssignment.UserId,
                        RoleId: existingAssignment.RoleId,
                        IsAssigned: true,
                        WasAlreadyAssigned: true));
            }

            UserRole newAssignment = UserRole.CreateNew(
                userId: payload.UserId,
                roleId: defaultRole.RoleId,
                assignedAt: payload.RegisteredAtUtc,
                assignedByUserId: null);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                UserRole createdAssignment = await _userRoleRepository.InsertAsync(
                    newAssignment,
                    cancellationToken);

                await _outboxWriter.EnqueueUserRoleAssignedAsync(
                    unitOfWork: _unitOfWork,
                    userId: createdAssignment.UserId,
                    roleId: defaultRole.RoleId,
                    rolePublicId: defaultRole.PublicId,
                    roleName: defaultRole.Name,
                    roleDisplayName: defaultRole.DisplayName,
                    roleIsSystem: defaultRole.IsSystem,
                    assignedByUserId: createdAssignment.AssignedByUserId,
                    assignedAtUtc: createdAssignment.AssignedAt,
                    correlationId: correlationId,
                    cancellationToken: cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result<IdentityUserRegisteredRoleAssignmentResult>.Success(
                    new IdentityUserRegisteredRoleAssignmentResult(
                        UserId: createdAssignment.UserId,
                        RoleId: createdAssignment.RoleId,
                        IsAssigned: true,
                        WasAlreadyAssigned: false));
            }
            catch (PersistenceException exception)
                when (string.Equals(
                    exception.Code,
                    "AUTHORIZATION.USER_ROLE_ALREADY_ASSIGNED",
                    StringComparison.Ordinal))
            {
                await _unitOfWork.RollbackAsync(cancellationToken);

                return Result<IdentityUserRegisteredRoleAssignmentResult>.Success(
                    new IdentityUserRegisteredRoleAssignmentResult(
                        UserId: payload.UserId,
                        RoleId: defaultRole.RoleId,
                        IsAssigned: true,
                        WasAlreadyAssigned: true));
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PersistenceException exception)
        {
            return Result<IdentityUserRegisteredRoleAssignmentResult>.Failure(
                MapPersistenceException(exception));
        }
        catch (AuthorizationDomainException exception)
        {
            return Result<IdentityUserRegisteredRoleAssignmentResult>.Failure(
                MapDomainException(exception));
        }
    }

    private static Error? ValidatePayload(
        string messageId,
        IdentityUserRegisteredPayload payload)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return Error.Validation(
                code: "AUTHORIZATION.IDENTITY_USER_REGISTERED_MESSAGE_ID_REQUIRED",
                message: "Identity user registered message id is required.");
        }

        if (payload.UserId <= 0)
        {
            return AuthorizationErrors.UserRole.InvalidUserId;
        }

        if (string.IsNullOrWhiteSpace(payload.UserPublicId))
        {
            return Error.Validation(
                code: "AUTHORIZATION.IDENTITY_USER_REGISTERED_PUBLIC_ID_REQUIRED",
                message: "Identity user registered public id is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return Error.Validation(
                code: "AUTHORIZATION.IDENTITY_USER_REGISTERED_EMAIL_REQUIRED",
                message: "Identity user registered email is required.");
        }

        if (payload.RegisteredAtUtc == default)
        {
            return AuthorizationErrors.UserRole.InvalidAssignTime;
        }

        return null;
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

            _ => AuthorizationErrors.UnexpectedError
        };
    }

    private static Error MapDomainException(AuthorizationDomainException exception)
    {
        return exception.Code switch
        {
            "AUTHORIZATION.USER_ROLE_INVALID_USER_ID" =>
                AuthorizationErrors.UserRole.InvalidUserId,

            "AUTHORIZATION.USER_ROLE_INVALID_ROLE_ID" =>
                AuthorizationErrors.UserRole.InvalidRoleId,

            "AUTHORIZATION.USER_ROLE_INVALID_ASSIGN_TIME" =>
                AuthorizationErrors.UserRole.InvalidAssignTime,

            _ => AuthorizationErrors.ValidationFailed
        };
    }

    private static string NormalizeRoleName(string roleName)
    {
        return roleName.Trim().ToUpperInvariant();
    }
}
