using Audit.Application.Models.Queries.Dashboard;
using Audit.Domain.Constants.AuditLog;
using Audit.Domain.Constants.Events;
using FluentValidation;

namespace Audit.Application.UseCases.Dashboard.GetRecentRiskEvents;

public sealed class GetRecentRiskEventsQueryValidator
    : AbstractValidator<GetRecentRiskEventsQuery>
{
    public GetRecentRiskEventsQueryValidator()
    {
        RuleFor(x => x.SourceModule)
            .Must(value => value is null || AuditSourceModules.IsValid(value))
            .WithMessage("Audit source module is invalid.");

        RuleFor(x => x.RiskLevel)
            .Must(value => value is null || AuditRiskLevels.IsValid(value))
            .WithMessage("Audit risk level is invalid.");

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

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100);
    }

    private static bool BeUtc(DateTime? value)
    {
        return value is null || value.Value.Kind == DateTimeKind.Utc;
    }
}