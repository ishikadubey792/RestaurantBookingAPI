using Microsoft.AspNetCore.Identity;
using RestaurantBookingAPI.Exceptions;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;
using RestaurantBookingAPI.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RestaurantBookingAPI.Services
{
    public class UserService : IUserService
    {
        private const int OtpExpiryMinutes = 5;
        private const int MaxOtpAttempts = 5;

        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IPasswordResetOtpRepository _otpRepository;
        private readonly PasswordService _passwordService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UserService> _logger;
        private readonly IEmailService _emailService;

        public UserService(
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            IPasswordResetOtpRepository otpRepository,
            PasswordService passwordService,
            IWebHostEnvironment environment,
            ILogger<UserService> logger,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _otpRepository = otpRepository;
            _passwordService = passwordService;
            _environment = environment;
            _logger = logger;
            _emailService = emailService;
        }

        private static bool IsPasswordValid(string password)
        {
            return !string.IsNullOrEmpty(password)
                && password.Length >= 8
                && password.Length <= 128
                && Regex.IsMatch(
                    password,
                    @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9\s])\S{8,128}$"
                );
        }

        private static string HashOtp(string otp)
        {
            var bytes = Encoding.UTF8.GetBytes(otp);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }


        public async Task<User> RegisterAsync(
            User user,
            string password)
        {
            if (!IsPasswordValid(password))
            {
                throw new BadRequestException(
                    "Password must be 8-30 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character. Spaces are not allowed."
                );
            }

            var email = user.Email.Trim();

            var existingUser =
                await _userRepository.GetByEmailAsync(email);

            if (existingUser != null)
            {
                throw new BadRequestException(
                    "Email is already registered."
                );
            }

            user.Email = email;
            user.PasswordHash =
                _passwordService.HashPassword(user, password);

            user.Role = "Customer";
            user.IsActive = true;
            user.CreatedAt = DateTime.UtcNow;

            // Step 1: Save user in database
            var registeredUser =
                await _userRepository.AddAsync(user);

            // Step 2: Create and link the Customer record
            var customer = new Customer
            {
                UserId = registeredUser.Id,
                Name = registeredUser.Name,
                Email = registeredUser.Email,
                Phone = string.Empty
            };

            await _customerRepository.AddAsync(customer);

            // Step 3: Prepare confirmation email
            var subject =
                "Welcome to Restaurant Booking - Registration Successful";

            var body = $"""
    Hello {registeredUser.Name},

    Thank you for registering with Restaurant Booking!

    Your account has been created successfully.

    You can now log in and start exploring restaurants
    and making bookings.

    Regards,
    Restaurant Booking Team
    """;

            // Step 3: Send email without failing registration
            try
            {
                await _emailService.SendEmailAsync(
                    registeredUser.Email,
                    subject,
                    body
                );
            }
            catch (Exception ex)
            {
                // Email failure should not undo registration
                // Log this error in production
                Console.Error.WriteLine(
                    $"Registration email failed for user ID {registeredUser.Id}: {ex.Message}"
                );
            }

            return registeredUser;
        }




        public async Task<User?> LoginAsync(LoginRequest request)
        {
            var email = request?.Email?.Trim();
            var password = request?.Password;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var user =
                await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                return null;
            }


            var verificationResult = _passwordService.VerifyPassword(
                user,
                user.PasswordHash,
                password
            );

            // Incorrect password: reject login.
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Password is correct, but its stored hash needs upgrading.
            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordService.HashPassword(user, password);
            }


            // Reactivate the account after valid credentials
            user.IsActive = true;

            // Store timestamps in IST
            var currentTime =
                DateTime.UtcNow.AddHours(5).AddMinutes(30);

            user.LastLoginAt = currentTime;
            user.LastActivityAt = currentTime;

            await _userRepository.UpdateAsync(user);

            return user;
        }



        public async Task ForgotPasswordAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new BadRequestException(
                    "A valid email address is required."
                );
            }

            var normalizedEmail = email.Trim();
            var user =
                await _userRepository.GetByEmailAsync(normalizedEmail);

            // Use the same outward response for unknown emails.
            // In production, avoid revealing account existence.
            if (user == null || !user.IsActive)
            {
                return;
            }

            var previousOtp =
                await _otpRepository.GetLatestUnusedAsync(user.Id);

            if (previousOtp != null)
            {
                previousOtp.IsUsed = true;
                await _otpRepository.UpdateAsync(previousOtp);
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000)
                .ToString();

            var otpRecord = new PasswordResetOtp
            {
                UserId = user.Id,
                OtpHash = HashOtp(otp),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                IsUsed = false,
                IsVerified = false,
                Attempts = 0
            };

            await _otpRepository.AddAsync(otpRecord);

            var subject = "Restaurant Booking - Password Reset OTP";

            var body = $"""
                 Hello {user.Name},
                 We received a request to reset your password.
                 Your OTP is: {otp}
                 This OTP expires in 5 minutes.
                 If you did not request this password reset, please ignore this email.
                 Regards,
                 Restaurant Booking Team
               """;

            await _emailService.SendEmailAsync(
                user.Email,
                subject,
                body
            );

            _logger.LogInformation(
                "Password reset email send completed for user ID {UserId}",
                user.Id
             );

            // Development-only: do not expose OTP in API responses.
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation(
                    "Password reset OTP generated for user ID {UserId}. Development OTP: {Otp}",
                    user.Id,
                    otp
                );
            }
        }

        public async Task<bool> VerifyResetOtpAsync(
            string email,
            string otp)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(otp))
            {
                return false;
            }

            var user =
                await _userRepository.GetByEmailAsync(email.Trim());

            if (user == null || !user.IsActive)
            {
                return false;
            }

            var record =
                await _otpRepository.GetLatestUnusedAsync(user.Id);

            if (record == null ||
                record.IsUsed ||
                record.ExpiresAt <= DateTime.UtcNow ||
                record.Attempts >= MaxOtpAttempts)
            {
                return false;
            }

            record.Attempts++;

            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(record.OtpHash),
                    Convert.FromHexString(HashOtp(otp))))
            {
                if (record.Attempts >= MaxOtpAttempts)
                {
                    record.IsUsed = true;
                    record.IsVerified = false;
                }

                await _otpRepository.UpdateAsync(record);
                return false;
            }

            record.IsVerified = true;
            await _otpRepository.UpdateAsync(record);

            return true;
        }

        public async Task ResetPasswordAsync(
            string email,
            string otp,
            string newPassword)
        {
            if (!IsPasswordValid(newPassword))
            {
                throw new BadRequestException(
                    "Password must be 8-128 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character. Spaces are not allowed."
                );
            }

            var user =
                await _userRepository.GetByEmailAsync(email.Trim());

            if (user == null || !user.IsActive)
            {
                throw new BadRequestException(
                    "Invalid or expired password reset request."
                );
            }

            var record =
                await _otpRepository.GetLatestUnusedAsync(user.Id);

            if (record == null ||
                record.IsUsed ||
                !record.IsVerified ||
                record.ExpiresAt <= DateTime.UtcNow ||
                record.Attempts >= MaxOtpAttempts ||
                !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(record.OtpHash),
                    Convert.FromHexString(HashOtp(otp))))
            {
                throw new BadRequestException(
                    "Please verify a valid, unexpired OTP before resetting your password."
                );
            }

            user.PasswordHash =
                _passwordService.HashPassword(user, newPassword);

            await _userRepository.UpdateAsync(user);

            record.IsUsed = true;
            await _otpRepository.UpdateAsync(record);
        }
    }
}
