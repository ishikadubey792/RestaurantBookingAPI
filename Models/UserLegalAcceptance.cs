namespace RestaurantBookingAPI.Models
{
    public class UserLegalAcceptance
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int LegalDocumentId { get; set; }

        public DateTime AcceptedAt { get; set; }

        // Navigation properties.
        public User User { get; set; } = null!;

        public LegalDocument LegalDocument { get; set; } = null!;
    }
}