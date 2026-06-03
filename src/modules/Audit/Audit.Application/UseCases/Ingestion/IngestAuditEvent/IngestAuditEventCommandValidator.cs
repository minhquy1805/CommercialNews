using Audit.Application.Models.Commands.Ingestion;
using Audit.Domain.Constants.Common;
using FluentValidation;

namespace Audit.Application.UseCases.Ingestion.IngestAuditEvent;

public sealed class IngestAuditEventCommandValidator
    : AbstractValidator<IngestAuditEventCommand>
{
    public IngestAuditEventCommandValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .Length(AuditConstants.MessageIdLength);

        RuleFor(x => x.EventType)
            .NotEmpty()
            .MaximumLength(AuditConstants.MaxEventTypeLength);

        RuleFor(x => x.AggregateType)
            .NotEmpty()
            .MaximumLength(AuditConstants.MaxAggregateTypeLength);

        RuleFor(x => x.AggregateId)
            .NotEmpty()
            .MaximumLength(AuditConstants.MaxAggregateIdLength);

        RuleFor(x => x.AggregatePublicId)
            .Length(AuditConstants.PublicIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AggregatePublicId));

        RuleFor(x => x.AggregateVersion)
            .GreaterThanOrEqualTo(AuditConstants.MinVersion)
            .When(x => x.AggregateVersion.HasValue);

        RuleFor(x => x.PayloadJson)
            .NotEmpty();

        RuleFor(x => x.HeadersJson)
            .Must(BeNullOrNotWhiteSpace)
            .When(x => x.HeadersJson is not null)
            .WithMessage("HeadersJson must not be blank when provided.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(AuditConstants.MaxCorrelationIdLength)
            .When(x => !string.IsNullOrWhiteSpace(x.CorrelationId));

        RuleFor(x => x.InitiatorUserId)
            .GreaterThan(0)
            .When(x => x.InitiatorUserId.HasValue);

        RuleFor(x => x.Priority)
            .InclusiveBetween(
                AuditConstants.MinSourcePriority,
                AuditConstants.MaxSourcePriority);

        RuleFor(x => x.OccurredAtUtc)
            .NotEmpty()
            .Must(BeUtc)
            .WithMessage("OccurredAtUtc must be UTC.");

        RuleFor(x => x.PublishedAtUtc)
            .Must(BeUtc)
            .When(x => x.PublishedAtUtc.HasValue)
            .WithMessage("PublishedAtUtc must be UTC when provided.");

        RuleFor(x => x)
            .Must(x =>
                x.PublishedAtUtc is null ||
                x.PublishedAtUtc.Value >= x.OccurredAtUtc)
            .WithMessage("PublishedAtUtc must be greater than or equal to OccurredAtUtc.");

        RuleFor(x => x.ConsumerName)
            .NotEmpty()
            .MaximumLength(AuditConstants.MaxConsumerNameLength);
    }

    private static bool BeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc;
    }

    private static bool BeUtc(DateTime? value)
    {
        return value is null || value.Value.Kind == DateTimeKind.Utc;
    }

    private static bool BeNullOrNotWhiteSpace(string? value)
    {
        return value is null || !string.IsNullOrWhiteSpace(value);
    }
}