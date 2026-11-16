
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

        // Extracts the authenticated owner UserId from JWT.
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

        // GET: api/bookings/my-bookings
        // Retrieves bookings for the authenticated customer only, resolved securely from token.
        [HttpGet("my-bookings")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyBookings()
        {
            var customerId = await GetAuthenticatedCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }

            var bookings = await _service.GetByCustomerIdAsync(
                customerId.Value
            );

            return ApiResponseHelper.Ok(
                this,
                "Customer bookings fetched successfully.",
                bookings
            );
        }

        // GET: api/bookings/customer/1
        // Preserved for backward compatibility: supports caller sending either Customer.Id or User.Id.
        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized();
            }

            var authenticatedCustomerId =
                await GetAuthenticatedCustomerIdAsync();

            if (authenticatedCustomerId == null)
            {
                return Unauthorized();
            }

            // Prevent access to another customer's booking history.
            // Accepts caller's resolved Customer.Id or caller's User.Id.
            if (customerId != authenticatedCustomerId.Value && customerId != userId)
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
        // Retrieves bookings only for restaurants owned by the authenticated owner, with optional filters.
        [HttpGet("my-restaurant-bookings")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> GetMyRestaurantBookings(
            [FromQuery] int? restaurantId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? bookingDate = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
            {
                return Unauthorized();
            }

            var bookings = await _service.GetBookingsByRestaurantOwnerAsync(
                ownerId.Value,
                restaurantId,
                status,
                bookingDate,
                fromDate,
                toDate);

            return ApiResponseHelper.Ok(
                this,
                "Restaurant bookings fetched successfully.",
                bookings
            );
        }

        // GET: api/bookings/my-restaurant-bookings/1
        // Retrieves a single booking only if it belongs to a restaurant owned by the authenticated owner.
        [HttpGet("my-restaurant-bookings/{id}")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> GetMyRestaurantBookingById(int id)
        {
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
            {
                return Unauthorized();
            }

            var booking = await _service.GetByIdForOwnerAsync(id, ownerId.Value);

            if (booking == null)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Booking not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Restaurant booking fetched successfully.",
                booking
            );
        }

        // POST: api/bookings/1/confirm
        // Confirms a booking for the authenticated owner's restaurant.
        [HttpPost("{id}/confirm")]
        [HttpPost("{id}/owner-confirm")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
            {
                return Unauthorized();
            }

            var confirmed = await _service.ConfirmBookingByOwnerAsync(id, ownerId.Value);

            if (!confirmed)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Booking not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Booking confirmed successfully.",
                new { id, status = "Confirmed" }
            );
        }

        // POST: api/bookings/1/owner-cancel
        // Cancels a booking for the authenticated owner's restaurant.
        [HttpPost("{id}/owner-cancel")]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> OwnerCancelBooking(int id)
        {
            var ownerId = GetCurrentOwnerId();

            if (ownerId == null)
            {
                return Unauthorized();
            }

            var cancelled = await _service.CancelBookingByOwnerAsync(id, ownerId.Value);

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
    }
}
