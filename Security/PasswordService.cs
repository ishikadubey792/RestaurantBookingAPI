
using Microsoft.AspNetCore.Identity;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Security
{
    public class PasswordService
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        public string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        // Returns the actual verification result instead of only true/false.
        public PasswordVerificationResult VerifyPassword(
            User user,
            string passwordHash,
            string password)
        {
            return _passwordHasher.VerifyHashedPassword(
                user,
                passwordHash,
                password
            );
        }
    }
}
