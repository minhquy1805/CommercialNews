namespace Interaction.Application.Models.Results;

public sealed record ArticleLikeStateResult(
    string ArticlePublicId,
    bool Liked);