namespace Notifications.Application.Contracts.Services;

public static class NotificationServiceErrorCodes
{
    public const string NetworkTimeout = "NETWORK_TIMEOUT";
    public const string AmbiguousTimeout = "AMBIGUOUS_TIMEOUT";

    public const string ProviderTemporaryUnavailable = "PROVIDER_TEMPORARILY_UNAVAILABLE";
    public const string ProviderRejected = "PROVIDER_REJECTED";

    public const string Smtp421 = "SMTP_421";
    public const string Smtp451 = "SMTP_451";
    public const string Smtp550 = "SMTP_550";
    public const string Smtp553 = "SMTP_553";

    public const string TemplateKeyInvalid = "TEMPLATE_KEY_INVALID";
    public const string TemplateRenderFailed = "TEMPLATE_RENDER_FAILED";
    public const string UnsafeTemplateVariables = "UNSAFE_TEMPLATE_VARIABLES";
}