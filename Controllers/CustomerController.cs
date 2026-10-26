
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Common;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomerController(ICustomerService service)
        {
            _service = service;
        }

        // GET: api/customers
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _service.GetAllAsync();

            return ApiResponseHelper.Ok(
                this,
                "Customers fetched successfully.",
                customers
            );
        }

        // GET: api/customers/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
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

        // POST: api/customers
        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            var createdCustomer =
                await _service.CreateAsync(customer);

            return ApiResponseHelper.Ok(
                this,
                "Customer created successfully.",
                createdCustomer
            );
        }

        // PUT: api/customers/1
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            Customer customer)
        {
            var updated =
                await _service.UpdateAsync(id, customer);

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
        [HttpDelete("{id}")]
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
