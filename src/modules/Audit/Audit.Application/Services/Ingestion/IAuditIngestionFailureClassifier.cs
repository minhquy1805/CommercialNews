using Audit.Domain.ValueObjects.Ingestion;

namespace Audit.Application.Services.Ingestion;

public interface IAuditIngestionFailureClassifier
{
    AuditErrorInfo Classify(
        Exception exception);

    AuditErrorInfo RedactionBlocked(
        string? reason);

    AuditErrorInfo UnsupportedEventType(
        string? eventType);
}