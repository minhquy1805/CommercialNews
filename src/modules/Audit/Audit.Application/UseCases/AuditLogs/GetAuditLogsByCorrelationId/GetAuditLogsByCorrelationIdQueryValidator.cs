using Audit.Application.Models.Queries.AuditLogs;
using Audit.Domain.Constants.Common;
using FluentValidation;

namespace Audit.Application.UseCases.AuditLogs.GetAuditLogsByCorrelationId;

public sealed class GetAuditLogsByCorrelationIdQueryValidator
    : AbstractValidator<GetAuditLogsByCorrelationIdQuery>
{
    public GetAuditLogsByCorrelationIdQueryValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .MaximumLength(AuditConstants.MaxCorrelationIdLength);

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
    }

    private static bool BeUtc(DateTime? value)
    {
        return value is null || value.Value.Kind == DateTimeKind.Utc;
    }
}