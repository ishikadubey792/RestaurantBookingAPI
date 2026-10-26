using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Security;
using RestaurantBookingAPI.Common;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly JwtService _jwtService;

        public AuthController(
            IUserService userService,
            JwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var user = new User
            {
                Name = request.Name,
                Email = request.Email.Trim()
            };

            var registeredUser = await _userService.RegisterAsync(
                user,
                request.Password
            );

            var responseData = new
            {
                id = registeredUser.Id,
                name = registeredUser.Name,
                email = registeredUser.Email
            };

            return ApiResponseHelper.Ok(
                this,
                "User registered successfully.",
                (object)responseData
            );
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _userService.LoginAsync(request);

            if (user == null)
            {
                return ApiResponseHelper.Unauthorized(
                    this,
                    "Invalid email or password."
                );
            }

            var token = _jwtService.GenerateToken(user);

            var responseData = new
            {
                accessToken = token,
                tokenType = "Bearer",
                expiresIn = 1800
            };

            return ApiResponseHelper.Ok(
                this,
                "Login successful.",
                (object)responseData
            );
        }

        // Step 1: Request a password reset OTP
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordRequest request)
        {
            await _userService.ForgotPasswordAsync(request.Email);

            return ApiResponseHelper.Ok<object>(
                this,
                "If an account exists for this email, password reset instructions will be sent.",
                (object?)null
            );
        }

        // Step 2: Verify the OTP
        [HttpPost("verify-reset-otp")]
        public async Task<IActionResult> VerifyResetOtp(
            VerifyResetOtpRequest request)
        {
            var isValid = await _userService.VerifyResetOtpAsync(
                request.Email,
                request.Otp
            );

            if (!isValid)
            {
                return ApiResponseHelper.BadRequest(
                    this,
                    "Invalid or expired OTP."
                );
            }

            return ApiResponseHelper.Ok<object>(
                this,
                "OTP verified successfully.",
                (object?)null
            );
        }

        // Step 3: Reset the password after OTP verification
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordRequest request)
        {
            await _userService.ResetPasswordAsync(
                request.Email,
                request.Otp,
                request.NewPassword
            );

            return ApiResponseHelper.Ok<object>(
                this,
                "Password reset successfully.",
                (object?)null
            );
        }
    }
}