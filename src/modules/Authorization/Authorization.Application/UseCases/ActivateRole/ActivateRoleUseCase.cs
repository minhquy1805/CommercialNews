using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Abstractions.Execution;
using CommercialNews.BuildingBlocks.Abstractions.Time;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.Results;

namespace Authorization.Application.UseCases.ActivateRole
{
    public sealed class ActivateRoleUseCase : IActivateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IRequestContext _requestContext;

        public ActivateRoleUseCase(
            IRoleRepository roleRepository,
            IAuthorizationUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IRequestContext requestContext)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _requestContext = requestContext;
        }

        public async Task<Result<ActivateRoleResponseDto>> ExecuteAsync(
            ActivateRoleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                return Result<ActivateRoleResponseDto>.Failure(
                    AuthorizationErrors.Role.InvalidRoleId);
            }

            try
            {
                var role = await _roleRepository.GetByIdAsync(
                    request.RoleId,
                    cancellationToken);

                if (role is null)
                {
                    return Result<ActivateRoleResponseDto>.Failure(
                        AuthorizationErrors.Role.NotFound);
                }

                if (role.IsActive)
                {
                    return Result<ActivateRoleResponseDto>.Success(
                        new ActivateRoleResponseDto
                        {
                            RoleId = request.RoleId,
                            IsActivated = true,
                            WasAlreadyActivated = true
                        });
                }

                var nowUtc = _dateTimeProvider.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                role.Activate(
                    nowUtc,
                    actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var updatedRole = await _roleRepository.UpdateAsync(
                        role,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<ActivateRoleResponseDto>.Success(
                        new ActivateRoleResponseDto
                        {
                            RoleId = updatedRole.RoleId,
                            IsActivated = true,
                            WasAlreadyActivated = false
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
                return Result<ActivateRoleResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (AuthorizationDomainException exception)
            {
                return Result<ActivateRoleResponseDto>.Failure(
                    MapDomainException(exception));
            }
        }

        private static Error MapDomainException(AuthorizationDomainException exception)
        {
            return exception.Code switch
            {
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
                "AUTHORIZATION.ROLE_NOT_FOUND" =>
                    AuthorizationErrors.Role.NotFound,

                "AUTHORIZATION.ROLE_EXISTS" =>
                    AuthorizationErrors.Role.Exists,

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}