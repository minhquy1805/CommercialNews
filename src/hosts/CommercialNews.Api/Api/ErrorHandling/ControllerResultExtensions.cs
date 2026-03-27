using CommercialNews.BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.ErrorHandling
{
    public static class ControllerResultExtensions
    {
        public static IActionResult ToActionResult(
            this ControllerBase controller,
            Result result)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsSuccess)
            {
                return controller.NoContent();
            }

            return CreateFailureResult(controller, result.Error!);
        }

        public static IActionResult ToActionResult<T>(
            this ControllerBase controller,
            Result<T> result)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsSuccess)
            {
                return controller.Ok(result.Value);
            }

            return CreateFailureResult(controller, result.Error!);
        }

        public static IActionResult ToCreatedAtActionResult<T>(
            this ControllerBase controller,
            Result<T> result,
            string actionName,
            object? routeValues = null)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentNullException.ThrowIfNull(result);
            ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

            if (result.IsSuccess)
            {
                return controller.CreatedAtAction(actionName, routeValues, result.Value);
            }

            return CreateFailureResult(controller, result.Error!);
        }

        private static IActionResult CreateFailureResult(
            ControllerBase controller,
            Error error)
        {
            var response = ErrorResponseFactory.Create(controller.HttpContext, error);
            var statusCode = ErrorTypeHttpMapper.ToStatusCode(error.Type);

            return controller.StatusCode(statusCode, response);
        }
    }
}