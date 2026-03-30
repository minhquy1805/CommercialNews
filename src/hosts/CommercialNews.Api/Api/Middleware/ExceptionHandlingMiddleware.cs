using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.BuildingBlocks.Results;

namespace CommercialNews.Api.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Request was cancelled by the client. Method={Method}, Path={Path}, TraceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception occurred. Method={Method}, Path={Path}, TraceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(
                        "The response has already started, so the error response cannot be written. TraceId={TraceId}",
                        context.TraceIdentifier);

                    throw;
                }

                await WriteErrorResponseAsync(context);
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context)
        {
            var error = Error.Failure(
                "COMMON.INTERNAL_SERVER_ERROR",
                "An unexpected error occurred.");

            var response = ErrorResponseFactory.Create(context, error);
            var statusCode = ErrorTypeHttpMapper.ToStatusCode(error.Type);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}