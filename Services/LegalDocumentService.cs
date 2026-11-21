using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Services
{
    public class LegalDocumentService : ILegalDocumentService
    {
        private readonly ILegalDocumentRepository _legalDocumentRepository;

        public LegalDocumentService(
            ILegalDocumentRepository legalDocumentRepository)
        {
            _legalDocumentRepository = legalDocumentRepository;
        }

        // Fetches the currently active Terms and Conditions
        // and maps the database entity to an API response DTO.
        public async Task<LegalDocumentResponseDto?> GetActiveTermsAsync()
        {
            const string documentType = "TermsAndConditions";

            var document =
                await _legalDocumentRepository.GetActiveDocumentAsync(
                    documentType);

            if (document == null)
            {
                return null;
            }

            return new LegalDocumentResponseDto
            {
                Id = document.Id,
                Title = document.Title,
                Content = document.Content,
                Version = document.Version,
                EffectiveDate = document.EffectiveDate
            };
        }
    }
}