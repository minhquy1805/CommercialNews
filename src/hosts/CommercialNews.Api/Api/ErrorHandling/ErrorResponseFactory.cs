using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.BuildingBlocks.SharedKernel.Results;

namespace CommercialNews.Api.Api.ErrorHandling
{
    public static class ErrorResponseFactory
    {
        public static ApiErrorResponse Create(string traceId, Error error)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
            ArgumentNullException.ThrowIfNull(error);

            return new ApiErrorResponse
            {
                TraceId = traceId,
                Error = new ApiErrorBody
                {
                    Code = error.Code,
                    Message = error.Message,
                    Details = error.Details.ToArray()
                }
            };
        }

        public static ApiErrorResponse Create(HttpContext httpContext, Error error)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            return Create(GetTraceId(httpContext), error);
        }

        private static string GetTraceId(HttpContext httpContext)
        {
            return httpContext.TraceIdentifier;
        }
    }
}