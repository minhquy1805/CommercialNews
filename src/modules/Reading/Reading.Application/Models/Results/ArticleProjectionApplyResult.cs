using Reading.Domain.Constants;

namespace Reading.Application.Models.Results;

public sealed class ArticleProjectionApplyResult
{
    public bool Applied { get; init; }

    public string Decision { get; init; } = ProjectionApplyDecisions.Ignored;

    public long? PreviousSourceVersion { get; init; }

    public long IncomingSourceVersion { get; init; }
}