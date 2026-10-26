using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Restaurant> Restaurants { get; set; }

        public DbSet<RestaurantTable> RestaurantTables { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<OperatingHour> OperatingHours { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }

        public DbSet<Wishlist> Wishlists { get; set; }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Restaurant>()
                .ToTable("Restaurant");

            modelBuilder.Entity<RestaurantTable>()
                .ToTable("RestaurantTable");

            modelBuilder.Entity<Customer>()
                .ToTable("Customer");

            modelBuilder.Entity<Customer>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .ToTable("Booking");

            modelBuilder.Entity<OperatingHour>()
                .ToTable("OperatingHour");

            modelBuilder.Entity<User>().ToTable("Users");

            modelBuilder.Entity<PasswordResetOtp>()
                .ToTable("PasswordResetOtps");

            modelBuilder.Entity<PasswordResetOtp>()
                .HasOne(otp => otp.User)
                .WithMany()
                .HasForeignKey(otp => otp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetOtp>()
                .HasIndex(otp => new { otp.UserId, otp.IsUsed });


            modelBuilder.Entity<Wishlist>()
                .ToTable("Wishlists");

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Restaurant)
                .WithMany()
                .HasForeignKey(w => w.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate restaurant entries for the same user
            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.RestaurantId })
                .IsUnique();

            modelBuilder.Entity<Payment>()
                 .ToTable("Payments");

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique()
                .HasFilter("[TransactionId] IS NOT NULL");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.BookingId)
                .IsUnique();
        }


    }
}