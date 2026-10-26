
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Common;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _service;

        public BookingController(IBookingService service)
        {
            _service = service;
        }

        // GET: api/bookings
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Bookings fetched successfully.",
                bookings
            );
        }

        // GET: api/bookings/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _service.GetByIdAsync(id);

            if (booking == null)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Booking not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Booking fetched successfully.",
                booking
            );
        }

        // GET: api/bookings/customer/1
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var bookings =
                await _service.GetByCustomerIdAsync(customerId);

            return ApiResponseHelper.Ok(
                this,
                "Customer bookings fetched successfully.",
                bookings
            );
        }

        // POST: api/bookings
        [HttpPost]
        public async Task<IActionResult> Create(Booking booking)
        {
            var createdBooking =
                await _service.CreateAsync(booking);

            return ApiResponseHelper.Ok(
                this,
                "Booking created successfully.",
                createdBooking
            );
        }

        // PUT: api/bookings/1
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            Booking booking)
        {
            var updated =
                await _service.UpdateAsync(id, booking);

            if (!updated)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Booking not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Booking updated successfully.",
                new { id }
            );
        }


        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var cancelled = await _service.CancelAsync(id);

            if (!cancelled)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Booking not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Booking cancelled successfully.",
                new { id, status = "Cancelled" }
            );
        }


        // DELETE: api/bookings/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted =
                await _service.DeleteAsync(id);

            if (!deleted)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Booking not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Booking deleted successfully.",
                new { id }
            );
        }

        // GET: api/bookings/availability
        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailableTables(
            [FromQuery] CheckAvailabilityModel request)
        {
            var tables =
                await _service.GetAvailableTablesAsync(request);

            return ApiResponseHelper.Ok(
                this,
                "Available tables fetched successfully.",
                tables
            );
        }
    }
}
