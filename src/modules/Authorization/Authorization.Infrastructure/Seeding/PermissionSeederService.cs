using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Constants;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Infrastructure.Seeding;

public sealed class PermissionSeederService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PermissionSeederService(
        IPermissionRepository permissionRepository,
        IAuthorizationUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _permissionRepository = permissionRepository
            ?? throw new ArgumentNullException(nameof(permissionRepository));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publicIdGenerator = publicIdGenerator
            ?? throw new ArgumentNullException(nameof(publicIdGenerator));
        _dateTimeProvider = dateTimeProvider
            ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var definition in SystemPermissions.All)
            {
                var normalizedKey = Normalize(definition.Key);

                var existingPermission = await _permissionRepository.GetByKeyNormalizedAsync(
                    normalizedKey,
                    cancellationToken);

                if (existingPermission is not null)
                {
                    continue;
                }

                var newPermission = Permission.CreateNew(
                    publicId: _publicIdGenerator.NewId(),
                    key: definition.Key,
                    keyNormalized: normalizedKey,
                    module: definition.Module,
                    action: definition.Action,
                    description: definition.Description,
                    isSystem: definition.IsSystem,
                    nowUtc: nowUtc,
                    actorUserId: null);

                await _permissionRepository.InsertAsync(
                    newPermission,
                    cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}