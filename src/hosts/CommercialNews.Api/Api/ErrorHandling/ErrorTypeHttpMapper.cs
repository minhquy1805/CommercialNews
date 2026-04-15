using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Api.Api.ErrorHandling
{
    public static class ErrorTypeHttpMapper
    {
        public static int ToStatusCode(ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
                ErrorType.Failure => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}

