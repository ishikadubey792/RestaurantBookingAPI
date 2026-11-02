
using System.Security.Claims;
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

        // Get currently logged-in owner's ID from JWT
        private int GetCurrentOwnerId()
        {
            var ownerId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(ownerId, out int id))
            {
                throw new UnauthorizedAccessException(
                    "Invalid owner identity.");
            }

            return id;
        }

        // ==========================================
        // PUBLIC RESTAURANT APIs
        // ==========================================

        // GET: api/restaurants
        [AllowAnonymous]
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

        // GET: api/restaurants/4004
        [AllowAnonymous]
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

        // ==========================================
        // ADMIN ONLY APIs
        // ==========================================

        // POST: api/restaurants
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] Restaurant restaurant)
        {
            var createdRestaurant =
                await _service.CreateAsync(restaurant);

            return ApiResponseHelper.Ok(
                this,
                "Restaurant created successfully.",
                createdRestaurant
            );
        }

        // PUT: api/restaurants/4004
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] Restaurant restaurant)
        {
            var updated =
                await _service.UpdateAsync(id, restaurant);

            if (!updated)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Restaurant not found."
                );
            }

            return ApiResponseHelper.Ok<object>(
                this,
                "Restaurant updated successfully.",
                (object?)null
            );
        }

        // DELETE: api/restaurants/4004
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Restaurant not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Restaurant deleted successfully.",
                new { id }
            );
        }

        // ==========================================
        // RESTAURANT OWNER APIs
        // ==========================================

        // GET: api/restaurants/my-restaurants
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet("my-restaurants")]
        public async Task<IActionResult> GetMyRestaurants()
        {
            var ownerId = GetCurrentOwnerId();

            var restaurants =
                await _service.GetMyRestaurantsAsync(ownerId);

            return ApiResponseHelper.Ok(
                this,
                "Your restaurants fetched successfully.",
                restaurants
            );
        }

        // GET: api/restaurants/my-restaurants/4004
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet("my-restaurants/{id}")]
        public async Task<IActionResult> GetMyRestaurantById(int id)
        {
            var ownerId = GetCurrentOwnerId();

            var restaurant =
                await _service.GetMyRestaurantByIdAsync(
                    id,
                    ownerId
                );

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

        // PUT: api/restaurants/my-restaurants/4004
        [Authorize(Roles = "RestaurantOwner")]
        [HttpPut("my-restaurants/{id}")]
        public async Task<IActionResult> UpdateMyRestaurant(
            int id,
            [FromBody] Restaurant restaurant)
        {
            var ownerId = GetCurrentOwnerId();

            var updated =
                await _service.UpdateMyRestaurantAsync(
                    id,
                    ownerId,
                    restaurant
                );

            if (!updated)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Restaurant not found."
                );
            }

            return ApiResponseHelper.Ok<object>(
                this,
                "Restaurant updated successfully.",
                (object?)null
            );
        }

        // DELETE: api/restaurants/my-restaurants/4004
        [Authorize(Roles = "RestaurantOwner")]
        [HttpDelete("my-restaurants/{id}")]
        public async Task<IActionResult> DeleteMyRestaurant(int id)
        {
            var ownerId = GetCurrentOwnerId();

            var deleted =
                await _service.DeleteMyRestaurantAsync(
                    id,
                    ownerId
                );

            if (!deleted)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Restaurant not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Restaurant deleted successfully.",
                new { id }
            );
        }
    }
}
