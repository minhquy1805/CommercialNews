using Audit.Domain.Constants.Common;
using Audit.Domain.Constants.Events;
using Audit.Domain.Exceptions;

namespace Audit.Domain.Policies.Evidence;

public sealed record AuditActionClassificationResult
{
    public string Action { get; }
    public string? ActionCategory { get; }

    private AuditActionClassificationResult(
        string action,
        string? actionCategory)
    {
        Action = action;
        ActionCategory = actionCategory;
    }

    public static AuditActionClassificationResult Create(
        string? action,
        string? actionCategory)
    {
        var normalizedAction = action?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAction))
        {
            throw AuditDomainException.ActionRequired();
        }

        if (normalizedAction.Length > AuditConstants.MaxActionLength)
        {
            throw AuditDomainException.ActionTooLong();
        }

        var normalizedActionCategory = NormalizeOptional(actionCategory);

        if (normalizedActionCategory is not null &&
            normalizedActionCategory.Length > AuditConstants.MaxActionCategoryLength)
        {
            throw AuditDomainException.ActionCategoryTooLong();
        }

        if (normalizedActionCategory is not null &&
            !AuditActionCategories.IsValid(normalizedActionCategory))
        {
            throw AuditDomainException.ActionCategoryInvalid(normalizedActionCategory);
        }

        return new AuditActionClassificationResult(
            normalizedAction,
            normalizedActionCategory);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
