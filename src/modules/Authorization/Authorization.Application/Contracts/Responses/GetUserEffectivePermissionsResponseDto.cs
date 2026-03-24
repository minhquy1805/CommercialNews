namespace Authorization.Application.Contracts.Responses
{
    public sealed class GetUserEffectivePermissionsResponseDto
    {
        public long UserId { get; init; }
        public IReadOnlyList<EffectivePermissionItemDto> Permissions { get; init; } = Array.Empty<EffectivePermissionItemDto>();
    }
}

