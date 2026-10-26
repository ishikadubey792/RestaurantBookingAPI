namespace RestaurantBookingAPI.Exceptions
{
    public class BookingConflictException : Exception
    {
        public BookingConflictException(string message)
            : base(message)
        {
        }
    }
}