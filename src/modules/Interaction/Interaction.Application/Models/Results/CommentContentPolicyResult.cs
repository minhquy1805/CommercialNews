namespace Interaction.Application.Models.Results;

public sealed record CommentContentPolicyResult(
    bool IsAllowed)
{
    public static CommentContentPolicyResult Allowed()
    {
        return new CommentContentPolicyResult(
            IsAllowed: true);
    }

    public static CommentContentPolicyResult Blocked()
    {
        return new CommentContentPolicyResult(
            IsAllowed: false);
    }
}