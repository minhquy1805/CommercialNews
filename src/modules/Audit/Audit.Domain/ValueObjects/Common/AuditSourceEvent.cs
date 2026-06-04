using Audit.Domain.Constants.Common;
using Audit.Domain.Constants.Events;
using Audit.Domain.Exceptions;

namespace Audit.Domain.ValueObjects.Common;

public sealed record AuditSourceEvent
{
    public string MessageId { get; }
    public string EventType { get; }
    public int? EventVersion { get; }
    public string SourceModule { get; }
    public int? SourcePriority { get; }
    public DateTime SourceOccurredAtUtc { get; }
    public DateTime? SourcePublishedAtUtc { get; }

    private AuditSourceEvent(
        string messageId,
        string eventType,
        int? eventVersion,
        string sourceModule,
        int? sourcePriority,
        DateTime sourceOccurredAtUtc,
        DateTime? sourcePublishedAtUtc)
    {
        MessageId = messageId;
        EventType = eventType;
        EventVersion = eventVersion;
        SourceModule = sourceModule;
        SourcePriority = sourcePriority;
        SourceOccurredAtUtc = sourceOccurredAtUtc;
        SourcePublishedAtUtc = sourcePublishedAtUtc;
    }

    public static AuditSourceEvent Create(
        string? messageId,
        string? eventType,
        int? eventVersion,
        string? sourceModule,
        int? sourcePriority,
        DateTime sourceOccurredAtUtc,
        DateTime? sourcePublishedAtUtc = null)
    {
        var normalizedMessageId = messageId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedMessageId))
        {
            throw AuditDomainException.MessageIdRequired();
        }

        if (normalizedMessageId.Length != AuditConstants.MessageIdLength)
        {
            throw AuditDomainException.MessageIdInvalidLength();
        }

        var normalizedEventType = eventType?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEventType))
        {
            throw AuditDomainException.EventTypeRequired();
        }

        if (normalizedEventType.Length > AuditConstants.MaxEventTypeLength)
        {
            throw AuditDomainException.EventTypeTooLong();
        }

        var normalizedSourceModule = sourceModule?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSourceModule))
        {
            throw AuditDomainException.SourceModuleRequired();
        }

        if (normalizedSourceModule.Length > AuditConstants.MaxSourceModuleLength)
        {
            throw AuditDomainException.SourceModuleTooLong();
        }

        if (!AuditSourceModules.IsValid(normalizedSourceModule))
        {
            throw AuditDomainException.SourceModuleInvalid(normalizedSourceModule);
        }

        if (eventVersion is not null && eventVersion < AuditConstants.MinVersion)
        {
            throw AuditDomainException.EventVersionInvalid();
        }

        if (sourcePriority is not null &&
            (sourcePriority < AuditConstants.MinSourcePriority ||
             sourcePriority > AuditConstants.MaxSourcePriority))
        {
            throw AuditDomainException.SourcePriorityInvalid();
        }

        EnsureSourceOccurredAtUtc(sourceOccurredAtUtc);
        EnsureSourcePublishedAtUtc(sourcePublishedAtUtc, sourceOccurredAtUtc);

        return new AuditSourceEvent(
            normalizedMessageId,
            normalizedEventType,
            eventVersion,
            normalizedSourceModule,
            sourcePriority,
            sourceOccurredAtUtc,
            sourcePublishedAtUtc);
    }

    private static void EnsureSourceOccurredAtUtc(DateTime sourceOccurredAtUtc)
    {
        if (sourceOccurredAtUtc == default)
        {
            throw AuditDomainException.SourceOccurredAtUtcRequired();
        }

        if (sourceOccurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw AuditDomainException.TimestampMustBeUtc(nameof(sourceOccurredAtUtc));
        }
    }

    private static void EnsureSourcePublishedAtUtc(
        DateTime? sourcePublishedAtUtc,
        DateTime sourceOccurredAtUtc)
    {
        if (sourcePublishedAtUtc is null)
        {
            return;
        }

        if (sourcePublishedAtUtc.Value == default ||
            sourcePublishedAtUtc.Value.Kind != DateTimeKind.Utc ||
            sourcePublishedAtUtc.Value < sourceOccurredAtUtc)
        {
            throw AuditDomainException.SourcePublishedAtUtcInvalid();
        }
    }
}
