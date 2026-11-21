using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.IRepository
{
    public interface IUserLegalAcceptanceRepository
    {
        Task AddAsync(UserLegalAcceptance acceptance);

        Task<bool> HasAcceptedAsync(int userId, int legalDocumentId);

        Task<List<UserLegalAcceptance>> GetHistoryAsync(int userId);
    }
}