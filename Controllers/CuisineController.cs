using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.Services;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/cuisines")]
    public class CuisineController : ControllerBase
    {
        private readonly ICuisineService _service;

        public CuisineController(ICuisineService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllActive()
        {
            var cuisines = await _service.GetAllActiveAsync();

            return Ok(new
            {
                StatusCode = 200,
                Message = "Cuisines fetched successfully.",
                Data = cuisines
            });
        }
    }
}