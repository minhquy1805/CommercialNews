namespace Interaction.Domain.Constants;

public static class ModerationReasonCodes
{
    public const string Spam = "Spam";
    public const string Harassment = "Harassment";
    public const string HateSpeech = "HateSpeech";
    public const string Violence = "Violence";
    public const string SexualContent = "SexualContent";
    public const string PersonalInformation = "PersonalInformation";
    public const string Misinformation = "Misinformation";
    public const string OffTopic = "OffTopic";
    public const string PolicyViolation = "PolicyViolation";
    public const string Other = "Other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Spam,
        Harassment,
        HateSpeech,
        Violence,
        SexualContent,
        PersonalInformation,
        Misinformation,
        OffTopic,
        PolicyViolation,
        Other
    };

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return All.Contains(value);
    }

    public static bool RequiresNote(string? value)
    {
        return string.Equals(value, Other, StringComparison.OrdinalIgnoreCase);
    }
}