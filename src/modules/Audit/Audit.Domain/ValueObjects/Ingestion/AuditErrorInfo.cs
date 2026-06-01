using Audit.Domain.Constants.AuditIngestion;
using Audit.Domain.Constants.Common;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Ingestion;

public sealed record AuditErrorInfo
{
    public string? LastErrorCode { get; }
    public string? LastErrorMessage { get; }
    public string? LastErrorClass { get; }

    private AuditErrorInfo(
        string? lastErrorCode,
        string? lastErrorMessage,
        string? lastErrorClass)
    {
        LastErrorCode = lastErrorCode;
        LastErrorMessage = lastErrorMessage;
        LastErrorClass = lastErrorClass;
    }

    public static AuditErrorInfo Create(
        string? lastErrorCode,
        string? lastErrorMessage,
        string? lastErrorClass)
    {
        var normalizedErrorCode = NormalizeOptional(lastErrorCode);
        if (normalizedErrorCode is not null &&
            normalizedErrorCode.Length > AuditConstants.MaxErrorCodeLength)
        {
            throw AuditDomainException.ErrorCodeTooLong();
        }

        var normalizedErrorMessage = NormalizeOptional(lastErrorMessage);
        if (normalizedErrorMessage is not null &&
            normalizedErrorMessage.Length > AuditConstants.MaxErrorMessageLength)
        {
            throw AuditDomainException.ErrorMessageTooLong();
        }

        var normalizedErrorClass = NormalizeOptional(lastErrorClass);
        if (normalizedErrorClass is not null &&
            !AuditIngestionErrorClasses.IsValid(normalizedErrorClass))
        {
            throw AuditDomainException.ErrorClassInvalid(normalizedErrorClass);
        }

        return new AuditErrorInfo(
            normalizedErrorCode,
            normalizedErrorMessage,
            normalizedErrorClass);
    }

    public static AuditErrorInfo None()
    {
        return new AuditErrorInfo(
            lastErrorCode: null,
            lastErrorMessage: null,
            lastErrorClass: null);
    }

    public static AuditErrorInfo Transient(
        string? errorCode,
        string? errorMessage)
    {
        return Create(
            errorCode,
            errorMessage,
            AuditIngestionErrorClasses.Transient);
    }

    public static AuditErrorInfo Permanent(
        string? errorCode,
        string? errorMessage)
    {
        return Create(
            errorCode,
            errorMessage,
            AuditIngestionErrorClasses.Permanent);
    }

    public static AuditErrorInfo Ambiguous(
        string? errorCode,
        string? errorMessage)
    {
        return Create(
            errorCode,
            errorMessage,
            AuditIngestionErrorClasses.Ambiguous);
    }

    public static AuditErrorInfo Validation(
        string? errorCode,
        string? errorMessage)
    {
        return Create(
            errorCode,
            errorMessage,
            AuditIngestionErrorClasses.Validation);
    }

    public static AuditErrorInfo Policy(
        string? errorCode,
        string? errorMessage)
    {
        return Create(
            errorCode,
            errorMessage,
            AuditIngestionErrorClasses.Policy);
    }

    public static AuditErrorInfo Redaction(
        string? errorCode,
        string? errorMessage)
    {
        return Create(
            errorCode,
            errorMessage,
            AuditIngestionErrorClasses.Redaction);
    }

    public static AuditErrorInfo Unknown(
        string? errorCode,
        string? errorMessage)
    {
        return Create(
            errorCode,
            errorMessage,
            AuditIngestionErrorClasses.Unknown);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}