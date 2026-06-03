using Audit.Application.Models.Queries.Ingestion;
using Audit.Domain.Constants.Common;
using FluentValidation;

namespace Audit.Application.UseCases.Ingestion.GetAuditIngestionByMessageId;

public sealed class GetAuditIngestionByMessageIdQueryValidator
    : AbstractValidator<GetAuditIngestionByMessageIdQuery>
{
    public GetAuditIngestionByMessageIdQueryValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .Length(AuditConstants.MessageIdLength);
    }
}