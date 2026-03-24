namespace Authorization.Application.Contracts.Responses;

public sealed class CreatePermissionResponseDto
{
    public long PermissionId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NameNormalized { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Module { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public long? CreatedByUserId { get; init; }
}