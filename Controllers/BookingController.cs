
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Common;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _service;
        private readonly ICustomerRepository _customerRepository;

        // Constructor: Injects booking service and customer repository.
        public BookingController(
            IBookingService service,
            ICustomerRepository customerRepository)
        {
            _service = service;
            _customerRepository = customerRepository;
        }

        // Extracts the authenticated UserId from JWT
        // and resolves the corresponding Customer record.
        // This prevents users from supplying another CustomerId.
        private async Task<int?> GetAuthenticatedCustomerIdAsync()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(userIdValue, out int userId))
            {
                return null;
            }

            var customer = await _customerRepository
                .GetByUserIdAsync(userId);

            return customer?.Id;
        }

        // GET: api/bookings
        // General booking list is restricted to RestaurantOwner.
        // Customer users must use their own booking history endpoint.
        //[HttpGet]
        //[Authorize(Roles = "RestaurantOwner")]
        //public async Task<IActionResult> GetAll()
        //{
        //    var bookings = await _service.GetAllAsync();

        //    return ApiResponseHelper.Ok(
        //        this,
        //        "Bookings fetched successfully.",
        //        bookings
        //    );
        //}

        // GET: api/bookings/1
        // Retrieves a booking only if it belongs to the authenticated customer.
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetById(int id)
        {
            var customerId = await GetAuthenticatedCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }

            var booking = await _service.GetByIdAsync(
                id,
                customerId.Value
            );

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
        // CustomerId is resolved from JWT, not trusted from the URL.
        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var authenticatedCustomerId =
                await GetAuthenticatedCustomerIdAsync();

            if (authenticatedCustomerId == null)
            {
                return Unauthorized();
            }

            // Prevent access to another customer's booking history.
            if (customerId != authenticatedCustomerId.Value)
            {
                return Forbid();
            }

            var bookings = await _service.GetByCustomerIdAsync(
                authenticatedCustomerId.Value
            );

            return ApiResponseHelper.Ok(
                this,
                "Customer bookings fetched successfully.",
                bookings
            );
        }


        // POST: api/bookings
        // Creates a booking for the authenticated customer only.
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingRequest request)
        {
            var customerId = await GetAuthenticatedCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }

            var createdBooking = await _service.CreateAsync(
                request,
                customerId.Value
            );

            return ApiResponseHelper.Ok(
                this,
                "Booking created successfully.",
                createdBooking
            );
        }



        // PUT: api/bookings/1
        // Updates a booking only if it belongs to the authenticated customer.
        [HttpPut("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBookingRequest request)
        {
            var customerId = await GetAuthenticatedCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }

            var updated = await _service.UpdateAsync(
                id,
                request,
                customerId.Value
            );

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


        // POST: api/bookings/1/cancel
        // Cancels a booking only if it belongs to the authenticated customer.
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var customerId = await GetAuthenticatedCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }

            var cancelled = await _service.CancelAsync(
                id,
                customerId.Value
            );

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
        // Deletes a booking only if it belongs to the authenticated customer.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Delete(int id)
        {
            var customerId = await GetAuthenticatedCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }

            var deleted = await _service.DeleteAsync(
                id,
                customerId.Value
            );

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
        // Public endpoint: customers can check availability without logging in.
        [HttpGet("availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableTables(
            [FromQuery] CheckAvailabilityModel request)
        {
            var tables = await _service.GetAvailableTablesAsync(request);

            return ApiResponseHelper.Ok(
                this,
                "Available tables fetched successfully.",
                tables
            );
        }

        // GET: api/bookings/my-restaurant-bookings
        // Retrieves bookings only for restaurants owned by the authenticated owner.
        [HttpGet("my-restaurant-bookings")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> GetMyRestaurantBookings()
        {
            var ownerIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(ownerIdValue, out int ownerId))
            {
                return Unauthorized();
            }

            var bookings = await _service
                .GetBookingsByRestaurantOwnerAsync(ownerId);

            return ApiResponseHelper.Ok(
                this,
                "Restaurant bookings fetched successfully.",
                bookings
            );
        }
    }
}
