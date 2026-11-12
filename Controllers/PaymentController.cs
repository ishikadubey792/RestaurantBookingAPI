using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.Common;
using RestaurantBookingAPI.IServices;
using System.Security.Claims;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize(Roles = "Customer")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }
        

        [Authorize(Roles = "Customer")]
        [HttpPost("initiate/{bookingId:int}")]
        public async Task<IActionResult> InitiatePayment(int bookingId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Valid user identity was not found."
                });
            }

            var payment = await _paymentService.InitiatePaymentAsync(
                bookingId,
                userId
            );

            return ApiResponseHelper.Ok(
                this,
                "Payment initiated successfully.",
                payment
            );
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Valid user identity was not found."
                });
            }

            var payments =
                await _paymentService.GetPaymentHistoryAsync(userId);

            return ApiResponseHelper.Ok(
                this,
                "Payment history fetched successfully.",
                payments
            );
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("{paymentId:int}")]
        public async Task<IActionResult> GetPaymentById(int paymentId)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Valid user identity was not found."
                });
            }

            var payment = await _paymentService.GetPaymentByIdAsync(
                paymentId,
                userId);

            if (payment == null)
            {
                return NotFound(new
                {
                    message = "Payment not found."
                });
            }

            return ApiResponseHelper.Ok(
                this,
                "Payment details fetched successfully.",
                payment
            );
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("{paymentId:int}/process-demo")]
        public async Task<IActionResult> ProcessDemoPayment(
    int paymentId,
    [FromQuery] bool simulateSuccess)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Valid user identity was not found."
                });
            }

            var payment = await _paymentService.GetPaymentByIdAsync(
                paymentId,
                userId);

            if (payment == null)
            {
                return NotFound(new
                {
                    message = "Payment not found."
                });
            }

            var processedPayment =
                await _paymentService.ProcessDemoPaymentAsync(
                    paymentId,
                    userId,
                    simulateSuccess);

            return ApiResponseHelper.Ok(
                this,
                simulateSuccess
                    ? "Demo payment succeeded."
                    : "Demo payment failed.",
                processedPayment
            );
        }
    }

}
