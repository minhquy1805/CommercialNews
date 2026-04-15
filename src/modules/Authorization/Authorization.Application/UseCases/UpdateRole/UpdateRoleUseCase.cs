using Authorization.Application.Common;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.UpdateRole
{
    public sealed class UpdateRoleUseCase : IUpdateRoleUseCase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRequestContext _requestContext;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateRoleUseCase(
            IRoleRepository roleRepository,
            IRequestContext requestContext,
            IAuthorizationUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _roleRepository = roleRepository;
            _requestContext = requestContext;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<UpdateRoleResponseDto>> ExecuteAsync(
            UpdateRoleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.RoleId <= 0)
            {
                return Result<UpdateRoleResponseDto>.Failure(
                    AuthorizationErrors.Role.InvalidRoleId);
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<UpdateRoleResponseDto>.Failure(
                    AuthorizationErrors.Role.NameRequired);
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
                    description: request.Description,
                    nowUtc: nowUtc,
                    actorUserId: actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var updatedRole = await _roleRepository.UpdateAsync(
                        role,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<UpdateRoleResponseDto>.Success(
                        new UpdateRoleResponseDto
                        {
                            RoleId = updatedRole.RoleId,
                            PublicId = updatedRole.PublicId,
                            Name = updatedRole.Name,
                            NameNormalized = updatedRole.NameNormalized,
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

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}