using Audit.Application.Models.Queries.AuditLogs;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Common;
using Audit.Domain.Constants.Events;
using FluentValidation;

namespace Audit.Application.UseCases.AuditLogs.GetAuditLogList;

public sealed class GetAuditLogListQueryValidator
    : AbstractValidator<GetAuditLogListQuery>
{
    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "OccurredAtUtc",
            "IngestedAtUtc",
            "CreatedAtUtc",
            "SourceModule",
            "EventType",
            "Action",
            "ActionCategory",
            "ResourceType",
            "ResourceId",
            "ActorUserId",
            "ActorInternalId",
            "Outcome",
            "Severity",
            "RiskLevel"
        };

    private static readonly HashSet<string> AllowedSortDirections =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "asc",
            "desc"
        };

    public GetAuditLogListQueryValidator()
    {
        RuleFor(x => x.SourceModule)
            .Must(value => value is null || AuditSourceModules.IsValid(value))
            .WithMessage("Audit source module is invalid.");

        RuleFor(x => x.EventType)
            .MaximumLength(AuditConstants.MaxEventTypeLength)
            .When(x => !string.IsNullOrWhiteSpace(x.EventType));

        RuleFor(x => x.Action)
            .MaximumLength(AuditConstants.MaxActionLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Action));

        RuleFor(x => x.ActionCategory)
            .Must(value => value is null || AuditActionCategories.IsValid(value))
            .WithMessage("Audit action category is invalid.");

        RuleFor(x => x.ResourceType)
            .MaximumLength(AuditConstants.MaxResourceTypeLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ResourceType));

        RuleFor(x => x.ResourceId)
            .MaximumLength(AuditConstants.MaxResourceIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ResourceId));

        RuleFor(x => x.ActorUserId)
            .Length(AuditConstants.PublicIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ActorUserId));

        RuleFor(x => x.ActorInternalId)
            .GreaterThan(0)
            .When(x => x.ActorInternalId.HasValue);

        RuleFor(x => x.Outcome)
            .Must(value => value is null || AuditOutcomes.IsValid(value))
            .WithMessage("Audit outcome is invalid.");

        RuleFor(x => x.Severity)
            .Must(value => value is null || AuditSeverities.IsValid(value))
            .WithMessage("Audit severity is invalid.");

        RuleFor(x => x.RiskLevel)
            .Must(value => value is null || AuditRiskLevels.IsValid(value))
            .WithMessage("Audit risk level is invalid.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(AuditConstants.MaxCorrelationIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.FromUtc)
            .Must(BeUtc)
            .When(x => x.FromUtc.HasValue)
            .WithMessage("FromUtc must be UTC when provided.");

        RuleFor(x => x.ToUtc)
            .Must(BeUtc)
            .When(x => x.ToUtc.HasValue)
            .WithMessage("ToUtc must be UTC when provided.");

        RuleFor(x => x)
            .Must(x =>
                x.FromUtc is null ||
                x.ToUtc is null ||
                x.FromUtc.Value <= x.ToUtc.Value)
            .WithMessage("FromUtc must be less than or equal to ToUtc.");

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200);

        RuleFor(x => x.SortBy)
            .Must(value => value is null || AllowedSortFields.Contains(value))
            .WithMessage("SortBy is not supported.");

        RuleFor(x => x.SortDirection)
            .Must(value => value is null || AllowedSortDirections.Contains(value))
            .WithMessage("SortDirection must be either 'asc' or 'desc'.");

        RuleFor(x => x.MessageId)
            .Length(AuditConstants.MessageIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.MessageId));
    }

    private static bool BeUtc(DateTime? value)
    {
        return value is null || value.Value.Kind == DateTimeKind.Utc;
    }
}