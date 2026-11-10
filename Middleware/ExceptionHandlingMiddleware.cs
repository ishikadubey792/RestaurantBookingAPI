
using Microsoft.Data.SqlClient;
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
                await WriteErrorResponse(
                    context,
                    StatusCodes.Status404NotFound,
                    ex.Message);
            }
            catch (BadRequestException ex)
            {
                await WriteErrorResponse(
                    context,
                    StatusCodes.Status400BadRequest,
                    ex.Message);
            }
            catch (BookingConflictException ex)
            {
                await WriteErrorResponse(
                    context,
                    StatusCodes.Status409Conflict,
                    ex.Message);
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                // Convert the database booking conflict into HTTP 409.
                _logger.LogWarning(
                    ex,
                    "Database booking conflict detected.");

                await WriteErrorResponse(
                    context,
                    StatusCodes.Status409Conflict,
                    "This table is already booked for the selected time.");
            }
            catch (Exception ex)
            {
                // Log complete details internally.
                _logger.LogError(
                    ex,
                    "Unhandled exception occurred.");

                // Do not expose internal exception details to clients.
                await WriteErrorResponse(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred. Please try again later.");
            }
        }

        // Creates a consistent API error response.
        private static async Task WriteErrorResponse(
            HttpContext context,
            int statusCode,
            string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>(
                statusCode,
                message,
                null);

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
