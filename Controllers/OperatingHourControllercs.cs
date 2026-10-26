
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var operatingHours =
                await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Operating hours fetched successfully.",
                operatingHours
            );
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var operatingHour =
                await _service.GetByIdAsync(id);

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

        [HttpGet("restaurant/{restaurantId}")]
        public async Task<IActionResult> GetByRestaurantId(
            int restaurantId)
        {
            var operatingHours =
                await _service.GetByRestaurantIdAsync(
                    restaurantId
                );

            return ApiResponseHelper.Ok(
                this,
                "Operating hours fetched successfully.",
                operatingHours
            );
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            OperatingHour operatingHour)
        {
            var createdOperatingHour =
                await _service.CreateAsync(operatingHour);

            return ApiResponseHelper.Ok(
                this,
                "Operating hour created successfully.",
                createdOperatingHour
            );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            OperatingHour operatingHour)
        {
            var updated =
                await _service.UpdateAsync(
                    id,
                    operatingHour
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted =
                await _service.DeleteAsync(id);

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
