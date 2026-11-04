
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Common;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/restaurant-tables")]
    public class RestaurantTableController : ControllerBase
    {
        private readonly IRestaurantTableService _service;

        public RestaurantTableController(
            IRestaurantTableService service)
        {
            _service = service;
        }

        // Existing read endpoint - unchanged
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Restaurant tables fetched successfully.",
                tables
            );
        }

        // Existing read endpoint - unchanged
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var table = await _service.GetByIdAsync(id);

            if (table == null)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Restaurant table not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Restaurant table fetched successfully.",
                table
            );
        }

        // Existing read endpoint - unchanged
        [HttpGet("restaurant/{restaurantId}")]
        public async Task<IActionResult> GetByRestaurantId(
            int restaurantId)
        {
            var tables =
                await _service.GetByRestaurantIdAsync(restaurantId);

            return ApiResponseHelper.Ok(
                this,
                "Restaurant tables fetched successfully.",
                tables
            );
        }

        // Only RestaurantOwner can create tables
        [HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> Create(
            [FromBody] RestaurantTable table)
        {
            var ownerIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(ownerIdValue, out int ownerId))
            {
                return Unauthorized();
            }

            var createdTable =
                await _service.CreateAsync(table, ownerId);

            return ApiResponseHelper.Ok(
                this,
                "Restaurant table created successfully.",
                createdTable
            );
        }

        // Only RestaurantOwner can update tables
        [HttpPut("{id}")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] RestaurantTable table)
        {
            var ownerIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(ownerIdValue, out int ownerId))
            {
                return Unauthorized();
            }

            var updated =
                await _service.UpdateAsync(id, table, ownerId);

            if (!updated)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Restaurant table not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Restaurant table updated successfully.",
                new { id }
            );
        }

        // Only RestaurantOwner can delete tables
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
                    "Restaurant table not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Restaurant table deleted successfully.",
                new { id }
            );
        }
    }
}
