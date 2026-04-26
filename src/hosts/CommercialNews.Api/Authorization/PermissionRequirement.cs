using Microsoft.AspNetCore.Authorization;

namespace CommercialNews.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionKey);
        PermissionKey = permissionKey;
    }

    public string PermissionKey { get; }
}