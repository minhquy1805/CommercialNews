namespace Authorization.Application.Consumers.Identity;

public sealed record IdentityUserRegisteredRoleAssignmentResult(
    long UserId,
    long RoleId,
    bool IsAssigned,
    bool WasAlreadyAssigned);
