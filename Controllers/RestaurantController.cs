using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.Common;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/restaurants")]
    [Authorize]
    public class RestaurantController : ControllerBase
    {
        private readonly IRestaurantService _service;

        public RestaurantController(IRestaurantService service)
        {
            _service = service;
        }

        [HttpGet]

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var restaurants = await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Restaurants fetched successfully.",
                restaurants
            );
        }


        [HttpGet("{id}")]

        public async Task<IActionResult> GetById(int id)
        {
            var restaurant = await _service.GetByIdAsync(id);

            if (restaurant == null)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Restaurant not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Restaurant fetched successfully.",
                restaurant
            );
        }



        [HttpPost]
        public async Task<IActionResult> Create(Restaurant restaurant)
        {
            var createdRestaurant = await _service.CreateAsync(restaurant);

            return ApiResponseHelper.Ok(
                this,
                "Restaurant created successfully.",
                createdRestaurant
            );
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            Restaurant restaurant)
        {
            var updated =
                await _service.UpdateAsync(id, restaurant);

            if (!updated)
            {
                return NotFound();
            }

            return Ok(new
            {
                message = "Restaurant updated successfully."
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return ApiResponseHelper.Ok(
            this,
            "Restaurant deleted successfully.",
             new { id }
 );
        }
    }
}