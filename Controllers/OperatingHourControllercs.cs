
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

        // Public read endpoint
        [HttpGet]
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

        // Public read endpoint
        [HttpGet("restaurant/{restaurantId}")]
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
            var ownerIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(ownerIdValue, out int ownerId))
            {
                return Unauthorized();
            }

            var createdOperatingHour =
                await _service.CreateAsync(
                    operatingHour,
                    ownerId
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
            var ownerIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(ownerIdValue, out int ownerId))
            {
                return Unauthorized();
            }

            var updated =
                await _service.UpdateAsync(
                    id,
                    operatingHour,
                    ownerId
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
            var ownerIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(ownerIdValue, out int ownerId))
            {
                return Unauthorized();
            }

            var deleted =
                await _service.DeleteAsync(id, ownerId);

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
