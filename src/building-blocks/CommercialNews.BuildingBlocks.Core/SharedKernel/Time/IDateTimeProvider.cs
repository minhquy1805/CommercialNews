namespace CommercialNews.BuildingBlocks.SharedKernel.Time;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}