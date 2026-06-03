using Audit.Application.Models.Queries.Ingestion;
using Audit.Domain.Constants.Common;
using FluentValidation;

namespace Audit.Application.UseCases.Ingestion.GetAuditIngestionDetail;

public sealed class GetAuditIngestionDetailQueryValidator
    : AbstractValidator<GetAuditIngestionDetailQuery>
{
    public GetAuditIngestionDetailQueryValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty()
            .Length(AuditConstants.PublicIdLength);
    }
}