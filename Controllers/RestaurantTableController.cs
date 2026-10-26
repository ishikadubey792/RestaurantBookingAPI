
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost]
        public async Task<IActionResult> Create(
            RestaurantTable table)
        {
            var createdTable =
                await _service.CreateAsync(table);

            return ApiResponseHelper.Ok(
                this,
                "Restaurant table created successfully.",
                createdTable
            );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            RestaurantTable table)
        {
            var updated =
                await _service.UpdateAsync(id, table);

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

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
