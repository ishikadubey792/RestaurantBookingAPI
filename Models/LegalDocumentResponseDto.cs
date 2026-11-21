namespace RestaurantBookingAPI.Models
{
    public class LegalDocumentResponseDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public DateTime EffectiveDate { get; set; }
    }
}