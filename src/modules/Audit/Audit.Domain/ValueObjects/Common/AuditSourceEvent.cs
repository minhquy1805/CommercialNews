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

    private AuditSourceEvent(
        string messageId,
        string eventType,
        int? eventVersion,
        string sourceModule)
    {
        MessageId = messageId;
        EventType = eventType;
        EventVersion = eventVersion;
        SourceModule = sourceModule;
    }

    public static AuditSourceEvent Create(
        string? messageId,
        string? eventType,
        int? eventVersion,
        string? sourceModule)
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

        return new AuditSourceEvent(
            normalizedMessageId,
            normalizedEventType,
            eventVersion,
            normalizedSourceModule);
    }
}