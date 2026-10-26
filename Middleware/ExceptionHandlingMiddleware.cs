
using RestaurantBookingAPI.Common;
using RestaurantBookingAPI.Exceptions;

namespace RestaurantBookingAPI.Middleware
{
    public class ExceptionHandlingMiddleware
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
            catch (NotFoundException ex)
            {
                context.Response.StatusCode =
                    StatusCodes.Status404NotFound;

                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>(
                    StatusCodes.Status404NotFound,
                    ex.Message,
                    null
                );

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (BadRequestException ex)
            {
                context.Response.StatusCode =
                    StatusCodes.Status400BadRequest;

                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>(
                    StatusCodes.Status400BadRequest,
                    ex.Message,
                    null
                );

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (BookingConflictException ex)
            {
                context.Response.StatusCode =
                    StatusCodes.Status409Conflict;

                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>(
                    StatusCodes.Status409Conflict,
                    ex.Message,
                    null
                );

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred.");

                context.Response.StatusCode =
                    StatusCodes.Status500InternalServerError;

                context.Response.ContentType = "application/json";

                var response = new ApiResponse<object>(
                    StatusCodes.Status500InternalServerError,
                    $"Debug error: {ex.GetType().Name} - {ex.Message}",
                    null
                );

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
