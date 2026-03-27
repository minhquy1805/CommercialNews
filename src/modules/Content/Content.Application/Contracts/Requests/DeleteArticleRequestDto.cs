namespace Content.Application.Contracts.Requests
{
    public sealed class DeleteArticleRequestDto
    {
        public long ArticleId { get; init; }

        public int ExpectedVersion { get; init; }
    }
}

