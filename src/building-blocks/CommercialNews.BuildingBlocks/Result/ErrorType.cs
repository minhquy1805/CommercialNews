namespace CommercialNews.BuildingBlocks.Results;

public enum ErrorType
{
    Validation = 0,
    Unauthorized = 1,
    Forbidden = 2,
    NotFound = 3,
    Conflict = 4,
    RateLimited = 5,
    Failure = 6
}