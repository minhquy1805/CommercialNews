using Audit.Application.Models.Queries.Ingestion;
using Audit.Domain.Constants.AuditIngestion;
using Audit.Domain.Constants.Common;
using FluentValidation;

namespace Audit.Application.UseCases.Ingestion.GetAuditIngestionList;

public sealed class GetAuditIngestionListQueryValidator
    : AbstractValidator<GetAuditIngestionListQuery>
{
    private static readonly HashSet<string> AllowedSortFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SourceOccurredAtUtc",
            "SourcePublishedAtUtc",
            "FirstReceivedAtUtc",
            "LastAttemptAtUtc",
            "ProcessedAtUtc",
            "DeadLetteredAtUtc",
            "CreatedAtUtc",
            "UpdatedAtUtc",
            "AttemptCount",
            "Status",
            "EventType",
            "ConsumerName"
        };

    private static readonly HashSet<string> AllowedSortDirections =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "asc",
            "desc"
        };

    public GetAuditIngestionListQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(value => value is null || AuditIngestionStatuses.IsValid(value))
            .WithMessage("Audit ingestion status is invalid.");

        RuleFor(x => x.EventType)
            .MaximumLength(AuditConstants.MaxEventTypeLength)
            .When(x => !string.IsNullOrWhiteSpace(x.EventType));

        RuleFor(x => x.AggregateType)
            .MaximumLength(AuditConstants.MaxAggregateTypeLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AggregateType));

        RuleFor(x => x.AggregateId)
            .MaximumLength(AuditConstants.MaxAggregateIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AggregateId));

        RuleFor(x => x.AggregatePublicId)
            .Length(AuditConstants.PublicIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AggregatePublicId));

        RuleFor(x => x.CorrelationId)
            .MaximumLength(AuditConstants.MaxCorrelationIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.ConsumerName)
            .MaximumLength(AuditConstants.MaxConsumerNameLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ConsumerName));

        RuleFor(x => x.LastErrorClass)
            .Must(value => value is null || AuditIngestionErrorClasses.IsValid(value))
            .WithMessage("Audit ingestion error class is invalid.");

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
    }

    private static bool BeUtc(DateTime? value)
    {
        return value is null || value.Value.Kind == DateTimeKind.Utc;
    }
}