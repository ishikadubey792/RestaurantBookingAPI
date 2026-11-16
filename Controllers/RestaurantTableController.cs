
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

        // Restrict global table listing to Admin
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Restaurant tables fetched successfully.",
                tables
            );
        }

        // Public read endpoint for table details
        [HttpGet("{id}")]
        [AllowAnonymous]
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

        // Public read endpoint for restaurant tables
        [HttpGet("restaurant/{restaurantId}")]
        [AllowAnonymous]
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
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return Unauthorized();
            }

            var createdTable =
                await _service.CreateAsync(table, ownerId.Value);

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
            var ownerId = GetCurrentOwnerId();
            if (ownerId == null)
            {
                return Unauthorized();
            }

            var updated =
                await _service.UpdateAsync(id, table, ownerId.Value);

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
