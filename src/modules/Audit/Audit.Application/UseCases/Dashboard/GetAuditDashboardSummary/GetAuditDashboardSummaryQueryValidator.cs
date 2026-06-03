using Audit.Application.Models.Queries.Dashboard;
using Audit.Domain.Constants.Events;
using FluentValidation;

namespace Audit.Application.UseCases.Dashboard.GetAuditDashboardSummary;

public sealed class GetAuditDashboardSummaryQueryValidator
    : AbstractValidator<GetAuditDashboardSummaryQuery>
{
    public GetAuditDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.SourceModule)
            .Must(value => value is null || AuditSourceModules.IsValid(value))
            .WithMessage("Audit source module is invalid.");

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
    }

    private static bool BeUtc(DateTime? value)
    {
        return value is null || value.Value.Kind == DateTimeKind.Utc;
    }
}