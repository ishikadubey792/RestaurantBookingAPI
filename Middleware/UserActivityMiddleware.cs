
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;

namespace RestaurantBookingAPI.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UserActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            AppDbContext dbContext)
        {
            await _next(context);

            if (context.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            if (context.Response.StatusCode < 200 ||
                context.Response.StatusCode >= 400)
            {
                return;
            }

            var userIdValue = context.User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int userId))
            {
                return;
            }

            var lastActivityAt = DateTime.UtcNow
                .AddHours(5)
                .AddMinutes(30);

            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                EXEC dbo.UpdateUserLastActivity
                    @UserId = {userId},
                    @LastActivityAt = {lastActivityAt}");
        }
    }
}
