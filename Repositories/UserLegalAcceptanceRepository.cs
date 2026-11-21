using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class UserLegalAcceptanceRepository
        : IUserLegalAcceptanceRepository
    {
        private readonly AppDbContext _context;

        public UserLegalAcceptanceRepository(AppDbContext context)
        {
            _context = context;
        }

        // Saves the user's acceptance of a legal document version.
        public async Task AddAsync(UserLegalAcceptance acceptance)
        {
            await _context.UserLegalAcceptances.AddAsync(acceptance);
            await _context.SaveChangesAsync();
        }

        // Checks whether the user has already accepted this document version.
        public async Task<bool> HasAcceptedAsync(
            int userId,
            int legalDocumentId)
        {
            return await _context.UserLegalAcceptances
                .AnyAsync(a =>
                    a.UserId == userId &&
                    a.LegalDocumentId == legalDocumentId);
        }

        // Retrieves the user's legal document acceptance history.
        public async Task<List<UserLegalAcceptance>> GetHistoryAsync(
            int userId)
        {
            return await _context.UserLegalAcceptances
                .AsNoTracking()
                .Include(a => a.LegalDocument)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AcceptedAt)
                .ToListAsync();
        }
    }
}