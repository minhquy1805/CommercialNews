using CommercialNews.BuildingBlocks.Abstractions.Time;

namespace CommercialNews.BuildingBlocks.Time
{
    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}