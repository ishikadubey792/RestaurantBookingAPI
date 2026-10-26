
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.IServices;

namespace RestaurantBookingAPI.Controllers
{
    [Route("api/wishlist")]
    [ApiController]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // Get the logged-in user's ID from JWT
        private int? GetCurrentUserId()
        {
            var userId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userId, out int id))
            {
                return id;
            }

            return null;
        }

        // POST: api/wishlist/5
        [HttpPost("{restaurantId:int}")]
        public async Task<IActionResult> AddToWishlist(int restaurantId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await _wishlistService.AddToWishlistAsync(
                userId.Value, restaurantId);

            return Ok(new
            {
                message = "Restaurant added to wishlist successfully."
            });
        }

        // GET: api/wishlist
        [HttpGet]
        public async Task<IActionResult> GetMyWishlist()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var wishlist = await _wishlistService
                .GetMyWishlistAsync(userId.Value);

            return Ok(wishlist);
        }

        // DELETE: api/wishlist/5
        [HttpDelete("{restaurantId:int}")]
        public async Task<IActionResult> RemoveFromWishlist(
            int restaurantId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await _wishlistService.RemoveFromWishlistAsync(
                userId.Value, restaurantId);

            return Ok(new
            {
                message = "Restaurant removed from wishlist successfully."
            });
        }
    }
}
