namespace Interaction.Domain.Entities;

using Interaction.Domain.Exceptions;

public sealed class ArticleViewEvent
{
    public long ArticleViewEventId { get; private set; }

    public long ArticleId { get; private set; }
    public long? UserId { get; private set; }
    public string? VisitorKey { get; private set; }

    public DateTime ViewedAt { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private ArticleViewEvent()
    {
    }

    public static ArticleViewEvent Create(
        long articleId,
        long? userId,
        string? visitorKey,
        string? ipAddress,
        string? userAgent,
        DateTime nowUtc)
    {
        ValidateArticleId(articleId);
        ValidateUserId(userId);
        ValidateVisitorKey(visitorKey);
        ValidateIpAddress(ipAddress);
        ValidateUserAgent(userAgent);

        return new ArticleViewEvent
        {
            ArticleId = articleId,
            UserId = userId,
            VisitorKey = NormalizeOptional(visitorKey),
            ViewedAt = nowUtc,
            IpAddress = NormalizeOptional(ipAddress),
            UserAgent = NormalizeOptional(userAgent)
        };
    }

    public static ArticleViewEvent Rehydrate(
        long articleViewEventId,
        long articleId,
        long? userId,
        string? visitorKey,
        DateTime viewedAt,
        string? ipAddress,
        string? userAgent)
    {
        if (articleViewEventId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_EVENT_INVALID_ID",
                "Article view event id must be greater than zero.");
        }

        ValidateArticleId(articleId);
        ValidateUserId(userId);
        ValidateVisitorKey(visitorKey);
        ValidateIpAddress(ipAddress);
        ValidateUserAgent(userAgent);

        if (viewedAt == default)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_EVENT_INVALID_VIEWED_AT",
                "ViewedAt must be a valid UTC datetime.");
        }

        return new ArticleViewEvent
        {
            ArticleViewEventId = articleViewEventId,
            ArticleId = articleId,
            UserId = userId,
            VisitorKey = NormalizeOptional(visitorKey),
            ViewedAt = viewedAt,
            IpAddress = NormalizeOptional(ipAddress),
            UserAgent = NormalizeOptional(userAgent)
        };
    }

    private static void ValidateArticleId(long articleId)
    {
        if (articleId <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_EVENT_INVALID_ARTICLE_ID",
                "Article id must be greater than zero.");
        }
    }

    private static void ValidateUserId(long? userId)
    {
        if (userId.HasValue && userId.Value <= 0)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_EVENT_INVALID_USER_ID",
                "User id must be greater than zero when provided.");
        }
    }

    private static void ValidateVisitorKey(string? visitorKey)
    {
        if (!string.IsNullOrWhiteSpace(visitorKey) && visitorKey.Trim().Length > 100)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_EVENT_VISITOR_KEY_TOO_LONG",
                "VisitorKey must not exceed 100 characters.");
        }
    }

    private static void ValidateIpAddress(string? ipAddress)
    {
        if (!string.IsNullOrWhiteSpace(ipAddress) && ipAddress.Trim().Length > 64)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_EVENT_IP_ADDRESS_TOO_LONG",
                "IpAddress must not exceed 64 characters.");
        }
    }

    private static void ValidateUserAgent(string? userAgent)
    {
        if (!string.IsNullOrWhiteSpace(userAgent) && userAgent.Trim().Length > 512)
        {
            throw new InteractionDomainException(
                "INTERACTION.ARTICLE_VIEW_EVENT_USER_AGENT_TOO_LONG",
                "UserAgent must not exceed 512 characters.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}