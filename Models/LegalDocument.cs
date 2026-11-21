namespace RestaurantBookingAPI.Models
{
    public class LegalDocument
    {
        public int Id { get; set; }

        public string DocumentType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime EffectiveDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for user acceptance history.
        public ICollection<UserLegalAcceptance> UserAcceptances { get; set; }
            = new List<UserLegalAcceptance>();
    }
}