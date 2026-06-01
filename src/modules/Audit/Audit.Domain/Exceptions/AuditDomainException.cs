using CommercialNews.BuildingBlocks.Domain.Exceptions;

namespace Audit.Domain.Exceptions;

public sealed class AuditDomainException : DomainException
{
    private AuditDomainException(string code, string message)
        : base(code, message)
    {
    }

    private AuditDomainException(string code, string message, Exception innerException)
        : base(code, message, innerException)
    {
    }

    public static AuditDomainException MessageIdRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.MessageIdRequired,
            "Audit message id is required.");
    }

    public static AuditDomainException MessageIdInvalidLength()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.MessageIdInvalidLength,
            "Audit message id must be 26 characters.");
    }

    public static AuditDomainException EventTypeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.EventTypeRequired,
            "Audit event type is required.");
    }

    public static AuditDomainException EventTypeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.EventTypeTooLong,
            "Audit event type is too long.");
    }

    public static AuditDomainException SourceModuleRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourceModuleRequired,
            "Audit source module is required.");
    }

    public static AuditDomainException SourceModuleInvalid(string sourceModule)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourceModuleInvalid,
            $"Audit source module '{sourceModule}' is invalid.");
    }

    public static AuditDomainException SourceModuleTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SourceModuleTooLong,
            "Audit source module is too long.");
    }

    public static AuditDomainException EventVersionInvalid()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.EventVersionInvalid,
            "Audit event version must be greater than or equal to 1.");
    }

    public static AuditDomainException AggregateTypeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregateTypeTooLong,
            "Audit aggregate type is too long.");
    }

    public static AuditDomainException AggregateIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregateIdTooLong,
            "Audit aggregate id is too long.");
    }

    public static AuditDomainException AggregatePublicIdInvalidLength()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregatePublicIdInvalidLength,
            "Audit aggregate public id must be 26 characters.");
    }

    public static AuditDomainException AggregateVersionInvalid()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.AggregateVersionInvalid,
            "Audit aggregate version must be greater than or equal to 1.");
    }
    public static AuditDomainException CorrelationIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.CorrelationIdTooLong,
            "Audit correlation id is too long.");
    }

    public static AuditDomainException CausationIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.CausationIdTooLong,
            "Audit causation id is too long.");
    }

    public static AuditDomainException TraceIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.TraceIdTooLong,
            "Audit trace id is too long.");
    }

    public static AuditDomainException ActorTypeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorTypeRequired,
            "Audit actor type is required.");
    }

    public static AuditDomainException ActorTypeInvalid(string actorType)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorTypeInvalid,
            $"Audit actor type '{actorType}' is invalid.");
    }

    public static AuditDomainException ActorUserIdInvalidLength()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorUserIdInvalidLength,
            "Audit actor user id must be 26 characters.");
    }

    public static AuditDomainException ActorEmailTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorEmailTooLong,
            "Audit actor email is too long.");
    }

    public static AuditDomainException ActorDisplayNameTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ActorDisplayNameTooLong,
            "Audit actor display name is too long.");
    }

    public static AuditDomainException ResourceTypeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceTypeRequired,
            "Audit resource type is required.");
    }

    public static AuditDomainException ResourceTypeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceTypeTooLong,
            "Audit resource type is too long.");
    }

    public static AuditDomainException ResourceIdRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceIdRequired,
            "Audit resource id is required.");
    }

    public static AuditDomainException ResourceIdTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceIdTooLong,
            "Audit resource id is too long.");
    }

    public static AuditDomainException ResourceDisplayNameTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ResourceDisplayNameTooLong,
            "Audit resource display name is too long.");
    }

    public static AuditDomainException OutcomeRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.OutcomeRequired,
            "Audit outcome is required.");
    }

    public static AuditDomainException OutcomeInvalid(string outcome)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.OutcomeInvalid,
            $"Audit outcome '{outcome}' is invalid.");
    }

    public static AuditDomainException SeverityRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SeverityRequired,
            "Audit severity is required.");
    }

    public static AuditDomainException SeverityInvalid(string severity)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.SeverityInvalid,
            $"Audit severity '{severity}' is invalid.");
    }

    public static AuditDomainException RiskLevelRequired()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.RiskLevelRequired,
            "Audit risk level is required.");
    }

    public static AuditDomainException RiskLevelInvalid(string riskLevel)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.RiskLevelInvalid,
            $"Audit risk level '{riskLevel}' is invalid.");
    }

    public static AuditDomainException IpAddressTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.IpAddressTooLong,
            "Audit IP address is too long.");
    }

    public static AuditDomainException UserAgentTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.UserAgentTooLong,
            "Audit user agent is too long.");
    }

    public static AuditDomainException JsonPayloadInvalid(Exception innerException)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.JsonPayloadInvalid,
            "Audit JSON payload is invalid.",
            innerException);
    }

    public static AuditDomainException ErrorCodeTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ErrorCodeTooLong,
            "Audit error code is too long.");
    }

    public static AuditDomainException ErrorMessageTooLong()
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ErrorMessageTooLong,
            "Audit error message is too long.");
    }

    public static AuditDomainException ErrorClassInvalid(string errorClass)
    {
        return new AuditDomainException(
            AuditDomainErrorCodes.ErrorClassInvalid,
            $"Audit error class '{errorClass}' is invalid.");
    }
}