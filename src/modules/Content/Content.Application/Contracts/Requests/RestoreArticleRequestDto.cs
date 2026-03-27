namespace Content.Application.Contracts.Requests
{
    public sealed class RestoreArticleRequestDto
    {
        public long ArticleId { get; init; }

        public int ExpectedVersion { get; init; }
    }
}

