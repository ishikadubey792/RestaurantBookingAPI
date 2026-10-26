
using Microsoft.EntityFrameworkCore;
using RestaurantBookingAPI.Data;

namespace RestaurantBookingAPI.Services
{
    public class UserInactivityService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<UserInactivityService> _logger;

        public UserInactivityService(
            IServiceScopeFactory scopeFactory,
            ILogger<UserInactivityService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var dbContext = scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                    var nowIst = DateTime.UtcNow
                        .AddHours(5)
                        .AddMinutes(30);

                    var result = await dbContext.Database
                        .SqlQueryRaw<int>(
                            "EXEC dbo.DeactivateInactiveUsers @NowIst = {0}",
                            nowIst)
                        .ToListAsync(stoppingToken);

                    var deactivatedCount = result.FirstOrDefault();

                    if (deactivatedCount > 0)
                    {
                        _logger.LogInformation(
                            "{Count} users marked inactive due to 30 days of inactivity.",
                            deactivatedCount);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error while checking inactive users.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromHours(24),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
