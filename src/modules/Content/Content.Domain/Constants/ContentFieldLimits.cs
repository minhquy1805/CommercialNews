namespace Content.Domain.Constants;

public static class ContentFieldLimits
{
    public const int PublicIdLength = 26;

    public const int ArticleTitleMaxLength = 300;
    public const int ArticleSummaryMaxLength = 1000;

    public const int CategoryNameMaxLength = 200;
    public const int CategoryNameNormalizedMaxLength = 200;

    public const int TagNameMaxLength = 150;
    public const int TagNameNormalizedMaxLength = 150;
    public const int TagDescriptionMaxLength = 500;

    public const int LifecycleActionTypeMaxLength = 30;
    public const int LifecycleStatusMaxLength = 30;
    public const int LifecycleReasonMaxLength = 500;

    public const int CorrelationIdMaxLength = 100;
    public const int ChangeSummaryMaxLength = 300;
}
