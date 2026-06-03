using Audit.Application.Models.Queries.AuditLogs;
using Audit.Domain.Constants.Common;
using FluentValidation;

namespace Audit.Application.UseCases.AuditLogs.GetAuditLogByMessageId;

public sealed class GetAuditLogByMessageIdQueryValidator
    : AbstractValidator<GetAuditLogByMessageIdQuery>
{
    public GetAuditLogByMessageIdQueryValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .Length(AuditConstants.MessageIdLength);
    }
}