namespace Content.Application.Contracts.Requests
{
    public sealed class UnpublishArticleRequestDto
    {
        public long ArticleId { get; init; }

        public int ExpectedVersion { get; init; }

        public string Reason { get; init; } = string.Empty;
    }
}