using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Evidence;

public sealed record AuditResource
{
    public string ResourceType { get; }
    public string ResourceId { get; }
    public string? ResourceDisplayName { get; }

    private AuditResource(
        string resourceType,
        string resourceId,
        string? resourceDisplayName)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        ResourceDisplayName = resourceDisplayName;
    }

    public static AuditResource Create(
        string? resourceType,
        string? resourceId,
        string? resourceDisplayName)
    {
        var normalizedResourceType = resourceType?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedResourceType))
        {
            throw AuditDomainException.ResourceTypeRequired();
        }

        if (normalizedResourceType.Length > AuditConstants.MaxResourceTypeLength)
        {
            throw AuditDomainException.ResourceTypeTooLong();
        }

        var normalizedResourceId = resourceId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedResourceId))
        {
            throw AuditDomainException.ResourceIdRequired();
        }

        if (normalizedResourceId.Length > AuditConstants.MaxResourceIdLength)
        {
            throw AuditDomainException.ResourceIdTooLong();
        }

        var normalizedResourceDisplayName = NormalizeOptional(resourceDisplayName);
        if (normalizedResourceDisplayName is not null &&
            normalizedResourceDisplayName.Length > AuditConstants.MaxResourceDisplayNameLength)
        {
            throw AuditDomainException.ResourceDisplayNameTooLong();
        }

        return new AuditResource(
            normalizedResourceType,
            normalizedResourceId,
            normalizedResourceDisplayName);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}