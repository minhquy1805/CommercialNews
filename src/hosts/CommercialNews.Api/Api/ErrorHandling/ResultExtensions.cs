using CommercialNews.BuildingBlocks.Results;
using Microsoft.AspNetCore.Http;

namespace CommercialNews.Api.Api.ErrorHandling
{
    public static class ResultExtensions
    {
        public static IResult ToHttpResult(this Result result, HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(httpContext);

            if (result.IsSuccess)
            {
                return Results.NoContent();
            }

            return CreateFailureResult(httpContext, result.Error!);
        }

        public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(httpContext);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return CreateFailureResult(httpContext, result.Error!);
        }

        private static IResult CreateFailureResult(HttpContext httpContext, Error error)
        {
            var statusCode = ErrorTypeHttpMapper.ToStatusCode(error.Type);
            var response = ErrorResponseFactory.Create(httpContext, error);

            return Results.Json(response, statusCode: statusCode);
        }
    }
}

