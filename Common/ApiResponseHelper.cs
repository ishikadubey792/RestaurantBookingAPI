
using Microsoft.AspNetCore.Mvc;

namespace RestaurantBookingAPI.Common
{
    public static class ApiResponseHelper
    {
        public static IActionResult Ok<T>(
            ControllerBase controller,
            string message,
            T? data = default)
        {
            return controller.Ok(new ApiResponse<T>(
                StatusCodes.Status200OK,
                message,
                data
            ));
        }

        public static IActionResult BadRequest(
            ControllerBase controller,
            string message)
        {
            return controller.BadRequest(new ApiResponse<object>(
                StatusCodes.Status400BadRequest,
                message,
                null
            ));
        }

        public static IActionResult Unauthorized(
            ControllerBase controller,
            string message)
        {
            return controller.Unauthorized(new ApiResponse<object>(
                StatusCodes.Status401Unauthorized,
                message,
                null
            ));
        }

        public static IActionResult NotFound(
            ControllerBase controller,
            string message)
        {
            return controller.NotFound(new ApiResponse<object>(
                StatusCodes.Status404NotFound,
                message,
                null
            ));
        }
    }
}
