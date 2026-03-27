namespace Content.Application.Contracts.Requests
{
    public sealed class GetArticleRevisionByIdRequestDto
    {
        public long ArticleId { get; init; }

        public long RevisionId { get; init; }
    }
}