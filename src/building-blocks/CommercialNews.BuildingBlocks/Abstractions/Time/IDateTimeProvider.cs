namespace CommercialNews.BuildingBlocks.Abstractions.Time
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}

