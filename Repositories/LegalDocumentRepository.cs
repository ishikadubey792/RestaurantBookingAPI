using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Repositories
{
    public class LegalDocumentRepository : ILegalDocumentRepository
    {
        private readonly AppDbContext _context;

        public LegalDocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        // Fetches the currently active legal document of the requested type.
        public async Task<LegalDocument?> GetActiveDocumentAsync(
            string documentType)
        {
            return await _context.LegalDocuments
                .AsNoTracking()
                .Where(d => d.DocumentType == documentType && d.IsActive)
                .FirstOrDefaultAsync();
        }

        // Fetches a specific legal document version using its ID.
        public async Task<LegalDocument?> GetByIdAsync(int id)
        {
            return await _context.LegalDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        // Adds a new legal document to the database.
        public async Task AddAsync(LegalDocument document)
        {
            await _context.LegalDocuments.AddAsync(document);
            await _context.SaveChangesAsync();
        }

        // Updates an existing legal document.
        public async Task UpdateAsync(LegalDocument document)
        {
            _context.LegalDocuments.Update(document);
            await _context.SaveChangesAsync();
        }
    }
}