namespace Audit.Domain.Constants.Common;

public static class AuditConstants
{
    public const int PublicIdLength = 26;
    public const int MessageIdLength = 26;
    public const int HashLength = 64;

    public const int MaxEventTypeLength = 200;
    public const int MaxSourceModuleLength = 100;

    public const int MaxActionLength = 120;
    public const int MaxActionCategoryLength = 100;

    public const int MaxAggregateTypeLength = 100;
    public const int MaxAggregateIdLength = 100;

    public const int MaxResourceTypeLength = 100;
    public const int MaxResourceIdLength = 100;
    public const int MaxResourceDisplayNameLength = 300;

    public const int MaxActorEmailLength = 320;
    public const int MaxActorDisplayNameLength = 200;

    public const int MaxSummaryLength = 500;

    public const int MaxCorrelationIdLength = 100;
    public const int MaxCausationIdLength = 100;
    public const int MaxTraceIdLength = 100;

    public const int MaxIpAddressLength = 45;
    public const int MaxUserAgentLength = 500;

    public const int MaxConsumerNameLength = 150;

    public const int MaxErrorCodeLength = 100;
    public const int MaxErrorMessageLength = 2000;

    public const int MinSourcePriority = 1;
    public const int MaxSourcePriority = 9;

    public const int MinVersion = 1;
}