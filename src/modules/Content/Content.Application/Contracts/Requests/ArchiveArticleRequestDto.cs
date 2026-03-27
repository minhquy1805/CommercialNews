namespace Content.Application.Contracts.Requests
{
    public sealed class ArchiveArticleRequestDto
    {
        public long ArticleId { get; init; }

        public int ExpectedVersion { get; init; }
    }
}

