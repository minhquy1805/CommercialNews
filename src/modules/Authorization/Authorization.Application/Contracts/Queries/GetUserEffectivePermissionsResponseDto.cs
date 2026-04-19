namespace Authorization.Application.Contracts.Queries;

public sealed class GetUserEffectivePermissionsResponseDto
{
    public long UserId { get; init; }
    public IReadOnlyList<EffectivePermissionItemDto> Permissions { get; init; } = Array.Empty<EffectivePermissionItemDto>();
}