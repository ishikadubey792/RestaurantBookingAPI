
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class PasswordResetOtpRepository : IPasswordResetOtpRepository
    {
        private readonly AppDbContext _context;

        public PasswordResetOtpRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetOtp> AddAsync(
            PasswordResetOtp otp)
        {
            var result = await _context.PasswordResetOtps
                .FromSqlInterpolated($@"
                    EXEC dbo.CreatePasswordResetOtp
                        @UserId = {otp.UserId},
                        @OtpHash = {otp.OtpHash},
                        @ExpiresAt = {otp.ExpiresAt},
                        @IsUsed = {otp.IsUsed},
                        @Attempts = {otp.Attempts},
                        @CreatedAt = {otp.CreatedAt},
                        @IsVerified = {otp.IsVerified}")
                .AsNoTracking()
                .ToListAsync();

            return result.First();
        }

        public async Task<PasswordResetOtp?> GetLatestUnusedAsync(
            int userId)
        {
            var result = await _context.PasswordResetOtps
                .FromSqlInterpolated($@"
                    EXEC dbo.GetLatestUnusedPasswordResetOtp
                        @UserId = {userId}")
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task UpdateAsync(PasswordResetOtp otp)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                EXEC dbo.UpdatePasswordResetOtp
                    @Id = {otp.Id},
                    @UserId = {otp.UserId},
                    @OtpHash = {otp.OtpHash},
                    @ExpiresAt = {otp.ExpiresAt},
                    @IsUsed = {otp.IsUsed},
                    @Attempts = {otp.Attempts},
                    @CreatedAt = {otp.CreatedAt},
                    @IsVerified = {otp.IsVerified}");
        }
    }
}
