namespace Authorization.Domain.Constants;

public sealed record SystemPermissionDefinition(
    string Key,
    string Module,
    string Action,
    string Description,
    bool IsSystem = true);