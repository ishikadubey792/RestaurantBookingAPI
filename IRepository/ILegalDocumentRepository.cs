using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface ILegalDocumentRepository
    {
        Task<LegalDocument?> GetActiveDocumentAsync(string documentType);

        Task<LegalDocument?> GetByIdAsync(int id);

        Task AddAsync(LegalDocument document);

        Task UpdateAsync(LegalDocument document);
    }
}