using Authorization.Application.Common;
using Authorization.Application.Contracts.Requests;
using Authorization.Application.Contracts.Responses;
using Authorization.Application.Errors;
using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using CommercialNews.BuildingBlocks.Persistence.Sql.Exceptions;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.RequestContext;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Application.UseCases.CreatePermission
{
    public sealed class CreatePermissionUseCase : ICreatePermissionUseCase
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRequestContext _requestContext;
        private readonly IPublicIdGenerator _publicIdGenerator;
        private readonly IAuthorizationUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreatePermissionUseCase(
            IPermissionRepository permissionRepository,
            IRequestContext requestContext,
            IPublicIdGenerator publicIdGenerator,
            IAuthorizationUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _permissionRepository = permissionRepository;
            _requestContext = requestContext;
            _publicIdGenerator = publicIdGenerator;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<CreatePermissionResponseDto>> ExecuteAsync(
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<CreatePermissionResponseDto>.Failure(
                    AuthorizationErrors.Permission.NameRequired);
            }

            try
            {
                var normalizedName = AuthorizationNameNormalizer.Normalize(request.Name);

                var existingPermission = await _permissionRepository.GetByNameNormalizedAsync(
                    normalizedName,
                    cancellationToken);

                if (existingPermission is not null)
                {
                    return Result<CreatePermissionResponseDto>.Failure(
                        AuthorizationErrors.Permission.Exists);
                }

                var nowUtc = _dateTimeProvider.UtcNow;
                var actorUserId = _requestContext.CurrentUserId;

                var permission = Permission.CreateNew(
                    publicId: _publicIdGenerator.NewId(),
                    name: request.Name.Trim(),
                    nameNormalized: normalizedName,
                    description: request.Description,
                    module: request.Module,
                    isSystem: request.IsSystem,
                    nowUtc: nowUtc,
                    actorUserId: actorUserId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    var createdPermission = await _permissionRepository.InsertAsync(
                        permission,
                        cancellationToken);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<CreatePermissionResponseDto>.Success(
                        new CreatePermissionResponseDto
                        {
                            PermissionId = createdPermission.PermissionId,
                            PublicId = createdPermission.PublicId,
                            Name = createdPermission.Name,
                            NameNormalized = createdPermission.NameNormalized,
                            Description = createdPermission.Description,
                            Module = createdPermission.Module,
                            IsSystem = createdPermission.IsSystem,
                            IsActive = createdPermission.IsActive,
                            CreatedAt = createdPermission.CreatedAt,
                            CreatedByUserId = createdPermission.CreatedByUserId
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
                return Result<CreatePermissionResponseDto>.Failure(
                    MapPersistenceException(exception));
            }
            catch (AuthorizationDomainException exception)
            {
                return Result<CreatePermissionResponseDto>.Failure(
                    MapDomainException(exception));
            }
        }

        private static Error MapDomainException(AuthorizationDomainException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.PERMISSION_PUBLIC_ID_REQUIRED" =>
                    AuthorizationErrors.Permission.PublicIdRequired,

                "AUTHORIZATION.PERMISSION_NAME_REQUIRED" =>
                    AuthorizationErrors.Permission.NameRequired,

                "AUTHORIZATION.PERMISSION_NAME_TOO_LONG" =>
                    AuthorizationErrors.Permission.NameTooLong,

                "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_REQUIRED" =>
                    AuthorizationErrors.Permission.NameNormalizedRequired,

                "AUTHORIZATION.PERMISSION_NAME_NORMALIZED_TOO_LONG" =>
                    AuthorizationErrors.Permission.NameNormalizedTooLong,

                "AUTHORIZATION.PERMISSION_MODULE_TOO_LONG" =>
                    AuthorizationErrors.Permission.ModuleTooLong,

                "AUTHORIZATION.PERMISSION_INVALID_PERMISSION_ID" =>
                    AuthorizationErrors.Permission.InvalidPermissionId,

                "AUTHORIZATION.PERMISSION_INVALID_TIMESTAMP" =>
                    AuthorizationErrors.Permission.InvalidTimestamp,

                "AUTHORIZATION.PERMISSION_STALE_UPDATE_TIME" =>
                    AuthorizationErrors.Permission.StaleUpdateTime,

                "AUTHORIZATION.SYSTEM_PERMISSION_PROTECTED" =>
                    AuthorizationErrors.Permission.SystemProtected,

                _ => AuthorizationErrors.ValidationFailed
            };
        }

        private static Error MapPersistenceException(PersistenceException exception)
        {
            return exception.Code switch
            {
                "AUTHORIZATION.PERMISSION_EXISTS" =>
                    AuthorizationErrors.Permission.Exists,

                "AUTHORIZATION.PERMISSION_NOT_FOUND" =>
                    AuthorizationErrors.Permission.NotFound,

                _ => AuthorizationErrors.ValidationFailed
            };
        }
    }
}