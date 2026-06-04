using Audit.Application.Models.Queries.AuditLogs;
using Audit.Domain.Constants.Common;
using FluentValidation;

namespace Audit.Application.UseCases.AuditLogs.GetAuditLogDetail;

public sealed class GetAuditLogDetailQueryValidator
    : AbstractValidator<GetAuditLogDetailQuery>
{
    public GetAuditLogDetailQueryValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty()
            .Length(AuditConstants.PublicIdLength);
    }
}