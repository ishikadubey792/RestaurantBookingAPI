using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RestaurantBookingAPI.Common;
using RestaurantBookingAPI.Controllers;
using RestaurantBookingAPI.IServices;
using RestaurantBookingAPI.Models;

namespace RestaurantBookingAPI.Tests.Unit;

public sealed class PaymentControllerTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly PaymentController _controller;

    public PaymentControllerTests()
    {
        _controller = new PaymentController(_paymentServiceMock.Object);
    }

    [Fact]
    public void Controller_RequiresCustomerRoleAuthorizeAttribute()
    {
        var attrs = typeof(PaymentController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .ToArray();

        Assert.NotEmpty(attrs);
        Assert.Contains(attrs, a => string.Equals(a.Roles, "Customer", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(nameof(PaymentController.InitiatePayment))]
    [InlineData(nameof(PaymentController.GetPaymentHistory))]
    [InlineData(nameof(PaymentController.GetPaymentById))]
    [InlineData(nameof(PaymentController.ProcessDemoPayment))]
    public void AllPaymentEndpoints_RequireCustomerRoleAuthorizeAttribute(string methodName)
    {
        var method = typeof(PaymentController).GetMethod(methodName);
        Assert.NotNull(method);
        var authorizeAttrs = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .ToArray();

        Assert.NotEmpty(authorizeAttrs);
        Assert.Contains(authorizeAttrs, a => string.Equals(a.Roles, "Customer", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(nameof(PaymentController.InitiatePayment), "Customer", true)]
    [InlineData(nameof(PaymentController.GetPaymentHistory), "Customer", true)]
    [InlineData(nameof(PaymentController.GetPaymentById), "Customer", true)]
    [InlineData(nameof(PaymentController.ProcessDemoPayment), "Customer", true)]
    [InlineData(nameof(PaymentController.InitiatePayment), "Admin", false)]
    [InlineData(nameof(PaymentController.GetPaymentHistory), "Admin", false)]
    [InlineData(nameof(PaymentController.GetPaymentById), "Admin", false)]
    [InlineData(nameof(PaymentController.ProcessDemoPayment), "Admin", false)]
    [InlineData(nameof(PaymentController.InitiatePayment), "RestaurantOwner", false)]
    [InlineData(nameof(PaymentController.GetPaymentHistory), "RestaurantOwner", false)]
    [InlineData(nameof(PaymentController.GetPaymentById), "RestaurantOwner", false)]
    [InlineData(nameof(PaymentController.ProcessDemoPayment), "RestaurantOwner", false)]
    [InlineData(nameof(PaymentController.InitiatePayment), null, false)]
    [InlineData(nameof(PaymentController.GetPaymentHistory), null, false)]
    [InlineData(nameof(PaymentController.GetPaymentById), null, false)]
    [InlineData(nameof(PaymentController.ProcessDemoPayment), null, false)]
    public async Task AllPaymentEndpoints_AuthorizePolicy_AllowsCustomerAndRejectsNonCustomerRoles(
        string methodName,
        string? role,
        bool expectedAllowed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        using var provider = services.BuildServiceProvider();
        var authService = provider.GetRequiredService<IAuthorizationService>();

        var method = typeof(PaymentController).GetMethod(methodName)!;
        var attributes = typeof(PaymentController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Concat(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true))
            .Cast<AuthorizeAttribute>()
            .ToArray();

        var policyBuilder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
        foreach (var attr in attributes)
        {
            if (!string.IsNullOrWhiteSpace(attr.Roles))
            {
                var roles = attr.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                policyBuilder.AddRequirements(new RolesAuthorizationRequirement(roles));
            }
        }

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "42") };
        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var result = await authService.AuthorizeAsync(principal, null, policyBuilder.Build());

        Assert.Equal(expectedAllowed, result.Succeeded);
    }

    [Fact]
    public async Task InitiatePayment_AuthenticatedCustomer_ReturnsOk()
    {
        SetAuthenticatedUser(userId: 42, role: "Customer");
        var payment = new Payment { Id = 5, BookingId = 10, Amount = 300m, Status = "Pending" };
        _paymentServiceMock.Setup(s => s.InitiatePaymentAsync(10, 42)).ReturnsAsync(payment);

        var result = await _controller.InitiatePayment(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<Payment>>(ok.Value);
        Assert.Equal(200, apiResponse.StatusCode);
        Assert.Equal(5, apiResponse.Data!.Id);
    }

    [Fact]
    public async Task GetPaymentById_AuthenticatedCustomerOwner_ReturnsOk()
    {
        SetAuthenticatedUser(userId: 42, role: "Customer");
        var payment = new Payment { Id = 20, BookingId = 10, Amount = 200m, Status = "Pending" };
        _paymentServiceMock.Setup(s => s.GetPaymentByIdAsync(20, 42)).ReturnsAsync(payment);

        var result = await _controller.GetPaymentById(20);

        var ok = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<Payment>>(ok.Value);
        Assert.Equal(200, apiResponse.StatusCode);
        Assert.Equal(20, apiResponse.Data!.Id);
    }

    [Fact]
    public async Task ProcessDemoPayment_AuthenticatedCustomerOwner_ReturnsOk()
    {
        SetAuthenticatedUser(userId: 42, role: "Customer");
        var pending = new Payment { Id = 20, BookingId = 10, Amount = 200m, Status = "Pending" };
        var succeeded = new Payment
        {
            Id = 20,
            BookingId = 10,
            Amount = 200m,
            Status = "Success",
            TransactionId = "DEMO-123"
        };
        _paymentServiceMock.Setup(s => s.GetPaymentByIdAsync(20, 42)).ReturnsAsync(pending);
        _paymentServiceMock.Setup(s => s.ProcessDemoPaymentAsync(20, 42, true)).ReturnsAsync(succeeded);

        var result = await _controller.ProcessDemoPayment(20, simulateSuccess: true);

        var ok = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<Payment>>(ok.Value);
        Assert.Equal(200, apiResponse.StatusCode);
        Assert.Equal("Success", apiResponse.Data!.Status);
    }

    [Fact]
    public async Task GetPaymentById_NonOwnedOrMissing_ReturnsNotFound()
    {
        SetAuthenticatedUser(userId: 77);
        _paymentServiceMock.Setup(s => s.GetPaymentByIdAsync(20, 77)).ReturnsAsync((Payment?)null);

        var result = await _controller.GetPaymentById(20);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ProcessDemoPayment_NonOwnedOrMissing_ReturnsNotFoundWithoutCallingProcess()
    {
        SetAuthenticatedUser(userId: 77);
        _paymentServiceMock.Setup(s => s.GetPaymentByIdAsync(20, 77)).ReturnsAsync((Payment?)null);

        var result = await _controller.ProcessDemoPayment(20, simulateSuccess: true);

        Assert.IsType<NotFoundObjectResult>(result);
        _paymentServiceMock.Verify(
            s => s.ProcessDemoPaymentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPaymentHistory_PassesAuthenticatedUserIdOnly()
    {
        SetAuthenticatedUser(userId: 42);
        var expected = new List<Payment>
        {
            new() { Id = 1, BookingId = 10, Amount = 200m, Status = "Pending" }
        };
        _paymentServiceMock.Setup(s => s.GetPaymentHistoryAsync(42)).ReturnsAsync(expected);

        var result = await _controller.GetPaymentHistory();

        var ok = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<List<Payment>>>(ok.Value);
        Assert.Equal(200, apiResponse.StatusCode);
        Assert.Single(apiResponse.Data!);
        _paymentServiceMock.Verify(s => s.GetPaymentHistoryAsync(42), Times.Once);
    }

    private void SetAuthenticatedUser(int userId, string role = "Customer")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}

