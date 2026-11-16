using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.Common;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomerController(ICustomerService service)
        {
            _service = service;
        }

        // Resolves the authenticated User's identity from the JWT NameIdentifier claim.
        private int? GetAuthenticatedUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out int userId))
            {
                return userId;
            }

            return null;
        }

        // Resolves the Customer domain record linked to the authenticated User.
        private async Task<Customer?> GetAuthenticatedCustomerAsync()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return null;
            }

            return await _service.GetByUserIdAsync(userId.Value);
        }

        // GET: api/customers
        // Restricted to Admin role only.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Customers fetched successfully.",
                customers
            );
        }

        // GET: api/customers/me
        // Returns the profile of the currently authenticated Customer.
        [HttpGet("me")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMe()
        {
            var customer = await GetAuthenticatedCustomerAsync();

            if (customer == null)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Customer profile not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Customer profile fetched successfully.",
                customer
            );
        }

        // GET: api/customers/1
        // Admin can access any customer; Customer can only access their own record.
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            if (User.IsInRole("Admin"))
            {
                var customer = await _service.GetByIdAsync(id);

                if (customer == null)
                {
                    return ApiResponseHelper.NotFound(
                        this,
                        "Customer not found."
                    );
                }

                return ApiResponseHelper.Ok(
                    this,
                    "Customer fetched successfully.",
                    customer
                );
            }

            // Customer role ownership check
            var currentCustomer = await GetAuthenticatedCustomerAsync();
            if (currentCustomer == null)
            {
                return Unauthorized();
            }

            if (currentCustomer.Id != id)
            {
                return Forbid();
            }

            return ApiResponseHelper.Ok(
                this,
                "Customer fetched successfully.",
                currentCustomer
            );
        }

        // POST: api/customers
        // Direct customer creation is restricted to Admin. Normal registration uses /api/auth/register.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Customer customer)
        {
            var createdCustomer = await _service.CreateAsync(customer);

            return ApiResponseHelper.Ok(
                this,
                "Customer created successfully.",
                createdCustomer
            );
        }

        // PUT: api/customers/me
        // Updates the profile of the currently authenticated Customer.
        [HttpPut("me")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateMe([FromBody] Customer customer)
        {
            var currentCustomer = await GetAuthenticatedCustomerAsync();
            if (currentCustomer == null)
            {
                return Unauthorized();
            }

            // Enforce immutability of primary and foreign keys
            customer.Id = currentCustomer.Id;
            customer.UserId = currentCustomer.UserId;

            var updated = await _service.UpdateAsync(currentCustomer.Id, customer);
            if (!updated)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Customer profile not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Customer profile updated successfully.",
                new { id = currentCustomer.Id }
            );
        }

        // PUT: api/customers/1
        // Admin can update any record; Customer can only update their own record.
        [HttpPut("{id}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] Customer customer)
        {
            if (User.IsInRole("Admin"))
            {
                var existingCustomer = await _service.GetByIdAsync(id);
                if (existingCustomer == null)
                {
                    return ApiResponseHelper.NotFound(
                        this,
                        "Customer not found."
                    );
                }

                // Preserve immutable keys
                customer.Id = id;
                customer.UserId = existingCustomer.UserId;

                var adminUpdated = await _service.UpdateAsync(id, customer);
                if (!adminUpdated)
                {
                    return ApiResponseHelper.NotFound(
                        this,
                        "Customer not found."
                    );
                }

                return ApiResponseHelper.Ok(
                    this,
                    "Customer updated successfully.",
                    new { id }
                );
            }

            // Customer role ownership check
            var currentCustomer = await GetAuthenticatedCustomerAsync();
            if (currentCustomer == null)
            {
                return Unauthorized();
            }

            if (currentCustomer.Id != id)
            {
                return Forbid();
            }

            // Prevent changing Id or UserId
            customer.Id = currentCustomer.Id;
            customer.UserId = currentCustomer.UserId;

            var updated = await _service.UpdateAsync(id, customer);
            if (!updated)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Customer not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Customer updated successfully.",
                new { id }
            );
        }

        // DELETE: api/customers/1
        // Restricted to Admin role only.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return ApiResponseHelper.NotFound(
                    this,
                    "Customer not found."
                );
            }

            return ApiResponseHelper.Ok(
                this,
                "Customer deleted successfully.",
                new { id }
            );
        }
    }
}
