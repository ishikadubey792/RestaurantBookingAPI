using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IServices
{
    public interface ILegalDocumentService
    {
        Task<LegalDocumentResponseDto?> GetActiveTermsAsync();
    }
}