
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var users = await _context.Users
                .FromSqlInterpolated(
                    $"EXEC dbo.GetUserByEmail @Email = {email}")
                .AsNoTracking()
                .ToListAsync();

            return users.FirstOrDefault();
        }

        public async Task<User> AddAsync(User user)
        {
            var users = await _context.Users
                .FromSqlInterpolated($"""
                    EXEC dbo.CreateUser
                        @Name = {user.Name},
                        @Email = {user.Email},
                        @PasswordHash = {user.PasswordHash},
                        @Role = {user.Role},
                        @IsActive = {user.IsActive},
                        @CreatedAt = {user.CreatedAt},
                        @LastLoginAt = {user.LastLoginAt},
                        @LastActivityAt = {user.LastActivityAt}
                    """)
                .AsNoTracking()
                .ToListAsync();

            var createdUser = users.FirstOrDefault();

            if (createdUser == null)
            {
                throw new InvalidOperationException(
                    "The CreateUser procedure did not return the created user.");
            }

            return createdUser;
        }

        public async Task UpdateAsync(User user)
        {
            var users = await _context.Users
                .FromSqlInterpolated($"""
                    EXEC dbo.UpdateUser
                        @Id = {user.Id},
                        @Name = {user.Name},
                        @Email = {user.Email},
                        @PasswordHash = {user.PasswordHash},
                        @Role = {user.Role},
                        @IsActive = {user.IsActive},
                        @CreatedAt = {user.CreatedAt},
                        @LastLoginAt = {user.LastLoginAt},
                        @LastActivityAt = {user.LastActivityAt}
                    """)
                .AsNoTracking()
                .ToListAsync();

            if (users.Count == 0)
            {
                throw new KeyNotFoundException(
                    $"User with ID {user.Id} was not found.");
            }
        }
    }
}
