using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.Common;
using RestaurantBookingAPI.IServices;
using System.Security.Claims;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/owner/dashboard")]
    [Route("api/owner-dashboard")]
    public class OwnerDashboardController : ControllerBase
    {
        private readonly IOwnerDashboardService _dashboardService;

        public OwnerDashboardController(IOwnerDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdValue, out int id))
            {
                return null;
            }

            return id;
        }

        // GET: api/owner/dashboard
        // Returns owner-scoped aggregate analytics for all owned restaurants
        [HttpGet]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? restaurantId = null,
            [FromQuery] int? ownerId = null)
        {
            var authenticatedUserId = GetCurrentUserId();

            if (authenticatedUserId == null)
            {
                return Unauthorized();
            }

            int targetOwnerId;

            // RestaurantOwner can only view their own dashboard.
            // Client-supplied ownerId query parameter is ignored for RestaurantOwner.
            if (User.IsInRole("RestaurantOwner"))
            {
                targetOwnerId = authenticatedUserId.Value;
            }
            else if (User.IsInRole("Admin"))
            {
                // Admin can optionally inspect a specific owner's dashboard
                targetOwnerId = ownerId ?? authenticatedUserId.Value;
            }
            else
            {
                return Forbid();
            }

            var metrics = await _dashboardService.GetDashboardMetricsAsync(
                targetOwnerId,
                fromDate,
                toDate,
                restaurantId);

            return ApiResponseHelper.Ok(
                this,
                "Owner dashboard metrics fetched successfully.",
                metrics
            );
        }
    }
}

