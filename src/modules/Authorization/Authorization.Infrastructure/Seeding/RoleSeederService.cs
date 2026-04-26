using Authorization.Application.Ports.Persistence;
using Authorization.Domain.Constants;
using Authorization.Domain.Entities;
using CommercialNews.BuildingBlocks.SharedKernel.Identifiers;
using CommercialNews.BuildingBlocks.SharedKernel.Time;

namespace Authorization.Infrastructure.Seeding;

public sealed class RoleSeederService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IAuthorizationUnitOfWork _unitOfWork;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RoleSeederService(
        IRoleRepository roleRepository,
        IAuthorizationUnitOfWork unitOfWork,
        IPublicIdGenerator publicIdGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _roleRepository = roleRepository
            ?? throw new ArgumentNullException(nameof(roleRepository));
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
            foreach (var definition in BuiltInRoles)
            {
                var normalizedName = Normalize(definition.Name);

                var existingRole = await _roleRepository.GetByNameNormalizedAsync(
                    normalizedName,
                    cancellationToken);

                if (existingRole is not null)
                {
                    continue;
                }

                var newRole = Role.CreateNew(
                    publicId: _publicIdGenerator.NewId(),
                    name: definition.Name,
                    nameNormalized: normalizedName,
                    displayName: definition.DisplayName,
                    description: definition.Description,
                    isSystem: true,
                    nowUtc: nowUtc,
                    actorUserId: null);

                await _roleRepository.InsertAsync(
                    newRole,
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

    private static readonly IReadOnlyList<RoleSeedDefinition> BuiltInRoles =
    [
        new(
            Name: SystemRoles.Admin,
            DisplayName: "Administrator",
            Description: "Full administrative access across authorization, content, and SEO."),

        new(
            Name: SystemRoles.Moderator,
            DisplayName: "Moderator",
            Description: "Moderation-focused administrative access for operational content workflows."),

        new(
            Name: SystemRoles.Author,
            DisplayName: "Author",
            Description: "Editorial authoring access for creating and updating content."),

        new(
            Name: SystemRoles.User,
            DisplayName: "User",
            Description: "Baseline authenticated user role with no default admin permissions.")
    ];

    private sealed record RoleSeedDefinition(
        string Name,
        string DisplayName,
        string Description);
}