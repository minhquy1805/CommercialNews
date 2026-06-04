using Audit.Domain.Exceptions;
using Audit.Domain.ValueObjects.Ingestion;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Text.Json;

namespace Audit.Application.Services.Ingestion;

public sealed class AuditIngestionFailureClassifier : IAuditIngestionFailureClassifier
{
    public AuditErrorInfo Classify(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var errorCode = ResolveErrorCode(exception);
        var errorMessage = NormalizeErrorMessage(exception.Message);

        if (exception is AuditDomainException)
        {
            return AuditErrorInfo.Validation(
                errorCode,
                errorMessage);
        }

        if (exception is OperationCanceledException)
        {
            return AuditErrorInfo.Transient(
                errorCode,
                errorMessage);
        }

        if (exception is TimeoutException)
        {
            return AuditErrorInfo.Transient(
                errorCode,
                errorMessage);
        }

        if (exception is SqlException sqlException)
        {
            return ClassifySqlException(
                sqlException,
                errorCode,
                errorMessage);
        }

        if (exception is DbException)
        {
            return AuditErrorInfo.Transient(
                errorCode,
                errorMessage);
        }

        if (exception is JsonException)
        {
            return AuditErrorInfo.Validation(
                errorCode,
                errorMessage);
        }

        if (exception is ArgumentException)
        {
            return AuditErrorInfo.Validation(
                errorCode,
                errorMessage);
        }

        return AuditErrorInfo.Unknown(
            errorCode,
            errorMessage);
    }

    public AuditErrorInfo RedactionBlocked(
        string? reason)
    {
        return AuditErrorInfo.Redaction(
            errorCode: "AUDIT_REDACTION_BLOCKED",
            errorMessage: NormalizeErrorMessage(
                reason ?? "Audit redaction policy blocked the payload."));
    }

    public AuditErrorInfo UnsupportedEventType(
        string? eventType)
    {
        return AuditErrorInfo.Policy(
            errorCode: "AUDIT_UNSUPPORTED_EVENT_TYPE",
            errorMessage: string.IsNullOrWhiteSpace(eventType)
                ? "Audit event type is not supported."
                : $"Audit event type '{eventType.Trim()}' is not supported.");
    }

    private static AuditErrorInfo ClassifySqlException(
        SqlException exception,
        string errorCode,
        string errorMessage)
    {
        if (IsTransientSqlError(exception.Number))
        {
            return AuditErrorInfo.Transient(
                errorCode,
                errorMessage);
        }

        if (IsConstraintOrValidationSqlError(exception.Number))
        {
            return AuditErrorInfo.Validation(
                errorCode,
                errorMessage);
        }

        return AuditErrorInfo.Ambiguous(
            errorCode,
            errorMessage);
    }

    private static bool IsTransientSqlError(int number)
    {
        return number is
            -2 or      // Timeout
            1205 or    // Deadlock
            4060 or
            40197 or
            40501 or
            40613 or
            49918 or
            49919 or
            49920;
    }

    private static bool IsConstraintOrValidationSqlError(int number)
    {
        return number is
            2601 or    // Duplicate key
            2627 or    // Unique constraint
            547 or     // Constraint check / FK
            8152 or    // String or binary data would be truncated - old
            2628;      // String or binary data would be truncated - new
    }

    private static string ResolveErrorCode(
        Exception exception)
    {
        if (exception is AuditDomainException domainException)
        {
            return string.IsNullOrWhiteSpace(domainException.Code)
                ? "AUDIT_DOMAIN_ERROR"
                : domainException.Code;
        }

        if (exception is SqlException sqlException)
        {
            return $"SQL_{sqlException.Number}";
        }

        return exception.GetType().Name;
    }

    private static string NormalizeErrorMessage(
        string? message)
    {
        var normalized = message?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Audit ingestion failed.";
        }

        return normalized.Length <= 2000
            ? normalized
            : normalized[..2000];
    }
}