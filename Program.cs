
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RestaurantBookingAPI.Data;
using RestaurantBookingAPI.IRepository;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Middleware;
using RestaurantBookingAPI.Repositories;
using RestaurantBookingAPI.Security;
using RestaurantBookingAPI.Services;
using RestaurantBookingAPI.Validation;
using System.Text;

namespace RestaurantBookingAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Security services
            builder.Services.AddScoped<PasswordService>();
            builder.Services.AddScoped<JwtService>();

            // Repository registrations
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPasswordResetOtpRepository, PasswordResetOtpRepository>();

            builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();
            builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IOperatingHourRepository, OperatingHourRepository>();

            // Service registrations
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRestaurantService, RestaurantService>();
            builder.Services.AddScoped<IRestaurantTableService, RestaurantTableService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IOperatingHourService, OperatingHourService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Validators
            builder.Services.AddScoped<RestaurantValidator>();
            builder.Services.AddScoped<CustomerValidator>();
            builder.Services.AddScoped<RestaurantTableValidator>();
            builder.Services.AddScoped<BookingValidator>();
            builder.Services.AddScoped<OperatingHourValidator>();
            builder.Services.AddScoped<CheckAvailabilityValidator>();


            // cuisine
            builder.Services.AddScoped<ICuisineRepository, CuisineRepository>();
            builder.Services.AddScoped<ICuisineService, CuisineService>();

            // restaurant owner registration
            builder.Services.AddScoped<IRestaurantOwnerRegistrationRepository,RestaurantOwnerRegistrationRepository>();

            builder.Services.AddScoped<IRestaurantOwnerRegistrationService,RestaurantOwnerRegistrationService>();

            // Wishlist
            builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();


            // Payment
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();


            // JWT Authentication
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)
                        ),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();

            // Controllers
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Swagger with JWT authorization
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(
                    "Bearer",
                    new Microsoft.OpenApi.OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = Microsoft.OpenApi.ParameterLocation.Header,
                        Description = "Enter your JWT token here."
                    });

                options.AddSecurityRequirement(document =>
                    new Microsoft.OpenApi.OpenApiSecurityRequirement
                    {
                        {
                            new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                                "Bearer", document),
                            new List<string>()
                        }
                    });
            });

            builder.Services.AddHostedService<UserInactivityService>();

            var app = builder.Build();

            // Global exception handling
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<UserActivityMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}