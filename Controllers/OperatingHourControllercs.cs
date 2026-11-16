
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Common;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/operating-hours")]
    public class OperatingHourController : ControllerBase
    {
        private readonly IOperatingHourService _service;

        public OperatingHourController(
            IOperatingHourService service)
        {
            _service = service;
        }

        private int? GetCurrentOwnerId()
        {
            var ownerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(ownerIdValue, out int id))
            {
                return null;
            }

            return id;
        }

        // Restrict global operating hours listing to Admin
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var operatingHours = await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Operating hours fetched successfully.",
                operatingHours
            );
        }

        // Public read endpoint
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var operatingHour = await _service.GetByIdAsync(id);

            if (operatingHour == null)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Operating hour not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Operating hour fetched successfully.",
                operatingHour
            );
        }

        // Public read endpoint for restaurant operating hours
        [HttpGet("restaurant/{restaurantId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByRestaurantId(
            int restaurantId)
        {
            var operatingHours =
                await _service.GetByRestaurantIdAsync(restaurantId);

            return ApiResponseHelper.Ok(
                this,
                "Operating hours fetched successfully.",
                operatingHours
            );
        }

        // Only RestaurantOwner can create operating hours
        [HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> Create(
            [FromBody] OperatingHour operatingHour)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return Unauthorized();
            }

            var createdOperatingHour =
                await _service.CreateAsync(
                    operatingHour,
                    ownerId.Value
                );

            return ApiResponseHelper.Ok(
                this,
                "Operating hour created successfully.",
                createdOperatingHour
            );
        }

        // Only RestaurantOwner can update operating hours
        [HttpPut("{id}")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] OperatingHour operatingHour)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return Unauthorized();
            }

            var updated =
                await _service.UpdateAsync(
                    id,
                    operatingHour,
                    ownerId.Value
                );

            if (!updated)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Operating hour not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Operating hour updated successfully.",
                new { id }
            );
        }

        // Only RestaurantOwner can delete operating hours
        [HttpDelete("{id}")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> Delete(int id)
        {
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return Unauthorized();
            }

            var deleted =
                await _service.DeleteAsync(id, ownerId.Value);

            if (!deleted)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Operating hour not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Operating hour deleted successfully.",
                new { id }
            );
        }
    }
}
