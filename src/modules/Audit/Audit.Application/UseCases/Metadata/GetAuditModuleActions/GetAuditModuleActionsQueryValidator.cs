using Audit.Application.Models.Queries.Metadata;
using Audit.Domain.Constants.Events;
using FluentValidation;

namespace Audit.Application.UseCases.Metadata.GetAuditModuleActions;

public sealed class GetAuditModuleActionsQueryValidator
    : AbstractValidator<GetAuditModuleActionsQuery>
{
    public GetAuditModuleActionsQueryValidator()
    {
        RuleFor(x => x.SourceModule)
            .NotEmpty()
            .Must(AuditSourceModules.IsCurrentV1Baseline)
            .WithMessage("Audit source module is invalid.");
    }
}
