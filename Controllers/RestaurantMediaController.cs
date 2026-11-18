
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RestaurantBookingAPI.IServices;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/restaurants/{restaurantId:int}")]
    public class RestaurantMediaController : ControllerBase
    {
        private readonly IRestaurantMediaService _mediaService;

        public RestaurantMediaController(
            IRestaurantMediaService mediaService)
        {
            // Inject the service responsible for restaurant media operations.
            _mediaService = mediaService;
        }

        // Gets the currently logged-in owner's ID from JWT claims.
        private int? GetCurrentOwnerId()
        {
            var ownerId = User.FindFirstValue(
                ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(ownerId, out int id))
            {
                return null;
            }

            return id;
        }

        // Public endpoint: retrieves gallery images for a restaurant.
        [HttpGet("images")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRestaurantImages(int restaurantId)
        {
            var images = await _mediaService.GetRestaurantImagesAsync(
                restaurantId);

            return Ok(images);
        }



        // Allows an authenticated restaurant owner to upload a gallery image.
        [HttpPost("images")]
        [Authorize(Roles = "RestaurantOwner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRestaurantImage(
            int restaurantId,
             IFormFile image)
        {
            // Get the owner ID from the authenticated JWT token.
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
                return Unauthorized("Invalid owner authentication.");

            // Validate that an image was provided.
            if (image == null || image.Length == 0)
                return BadRequest("Please select an image to upload.");

            // Delegate ownership verification, file storage and database insertion to the service.
            var uploadedImage = await _mediaService.AddRestaurantImageAsync(
                restaurantId,
                ownerId.Value,
                image);

            return Ok(uploadedImage);
        }



        // Allows an authenticated restaurant owner to delete a gallery image.
        [HttpDelete("images/{imageId:int}")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> DeleteRestaurantImage(
            int restaurantId,
            int imageId)
        {
            // Get the owner ID from the authenticated JWT token.
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
                return Unauthorized("Invalid owner authentication.");

            // Delete the image only after the service verifies restaurant ownership.
            var deleted = await _mediaService.DeleteRestaurantImageAsync(
                restaurantId,
                imageId,
                ownerId.Value);

            if (!deleted)
                return NotFound("Restaurant image not found.");

            return Ok(new
            {
                message = "Restaurant image deleted successfully."
            });
        }


        // Allows an authenticated restaurant owner to select the primary gallery image.
        [HttpPut("images/{imageId:int}/primary")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> SetPrimaryImage(
            int restaurantId,
            int imageId)
        {
            // Get the owner ID from the authenticated JWT token.
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
                return Unauthorized("Invalid owner authentication.");

            // Verify ownership and update the restaurant's cover image.
            var updated = await _mediaService.SetPrimaryImageAsync(
                restaurantId,
                imageId,
                ownerId.Value);

            if (!updated)
                return NotFound("Restaurant image not found.");

            return Ok(new
            {
                message = "Primary restaurant image updated successfully."
            });
        }



        // Public endpoint: retrieves menu images for a restaurant.
        [HttpGet("menu-images")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenuImages(int restaurantId)
        {
            var menuImages = await _mediaService.GetMenuImagesAsync(
                restaurantId);

            return Ok(menuImages);
        }




        // Allows an authenticated restaurant owner to upload a menu image.
        [HttpPost("menu-images")]
        [Authorize(Roles = "RestaurantOwner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMenuImage(
            int restaurantId,
            IFormFile image)
        {
            // Get the owner ID from the authenticated JWT token.
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
                return Unauthorized("Invalid owner authentication.");

            // Validate that a menu image was provided.
            if (image == null || image.Length == 0)
                return BadRequest("Please select a menu image to upload.");

            // Verify ownership, save the image and create its database record.
            var uploadedImage = await _mediaService.AddMenuImageAsync(
                restaurantId,
                ownerId.Value,
                image);

            return Ok(uploadedImage);
        }


        // Allows an authenticated restaurant owner to delete a menu image.
        [HttpDelete("menu-images/{imageId:int}")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> DeleteMenuImage(
            int restaurantId,
            int imageId)
        {
            // Get the owner ID from the authenticated JWT token.
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
                return Unauthorized("Invalid owner authentication.");

            // Delete the menu image after verifying restaurant ownership.
            var deleted = await _mediaService.DeleteMenuImageAsync(
                restaurantId,
                imageId,
                ownerId.Value);

            if (!deleted)
                return NotFound("Menu image not found.");

            return Ok(new
            {
                message = "Menu image deleted successfully."
            });
        }


    }
}
