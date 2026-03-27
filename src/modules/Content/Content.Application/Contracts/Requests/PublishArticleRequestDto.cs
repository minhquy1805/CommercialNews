namespace Content.Application.Contracts.Requests
{
    public sealed class PublishArticleRequestDto
    {
        public long ArticleId { get; init; }

        public int ExpectedVersion { get; init; }
    }
}