namespace Authorization.Application.Contracts.Queries;

public sealed class GetUserEffectivePermissionsRequestDto
{
    public long UserId { get; init; }
}