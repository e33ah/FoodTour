using Microsoft.Extensions.DependencyInjection;

namespace FoodTour.Test.Integration;

public class PurchaseCodeValidationIntegrationTests {
    
    [Fact]
    public async Task PurchaseCodeValidationService_CanBeResolvedFromServiceProvider_Successfully() {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseSqlite(connection)
            .Options;

        services.AddSingleton(options);
        services.AddScoped<FoodTourContext>();
        services.AddScoped<IPurchaseCodeValidationService, PurchaseCodeValidationService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var purchaseCodeValidationService = scope.ServiceProvider.GetRequiredService<IPurchaseCodeValidationService>();

        purchaseCodeValidationService.ShouldNotBeNull();
        purchaseCodeValidationService.ShouldBeOfType<PurchaseCodeValidationService>();
    }

    [Fact]
    public async Task PurchaseCodeValidationService_WorksWithEntityFramework_InIntegrationScenario() {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseSqlite(connection)
            .Options;

        services.AddSingleton(options);
        services.AddScoped<FoodTourContext>();
        services.AddScoped<IPurchaseCodeValidationService, PurchaseCodeValidationService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FoodTourContext>();
        await context.Database.EnsureCreatedAsync();

        var purchaseCodeValidationService = scope.ServiceProvider.GetRequiredService<IPurchaseCodeValidationService>();

        // Add test data
        await context.PurchaseCodes.AddAsync(new PurchaseCode { Code = "TEST123", IsActive = true });
        await context.SaveChangesAsync();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "TEST123",
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        var result = await purchaseCodeValidationService.ValidateAsync(request);

        result.IsValid.ShouldBe(true);
        result.ErrorMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData(PurchaseCodeValidationType.MustExistAndBeActive)]
    [InlineData(PurchaseCodeValidationType.MustNotExist)]
    [InlineData(PurchaseCodeValidationType.MustExist)]
    [InlineData(PurchaseCodeValidationType.MustExistForCancellation)]
    public async Task PurchaseCodeValidationService_HandlesAllValidationTypes_InIntegrationScenario(PurchaseCodeValidationType validationType) {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseSqlite(connection)
            .Options;

        services.AddSingleton(options);
        services.AddScoped<FoodTourContext>();
        services.AddScoped<IPurchaseCodeValidationService, PurchaseCodeValidationService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FoodTourContext>();
        await context.Database.EnsureCreatedAsync();

        var purchaseCodeValidationService = scope.ServiceProvider.GetRequiredService<IPurchaseCodeValidationService>();

        // Setup test data based on validation type
        string testCode = $"TEST_{validationType}";
        await SetupTestDataForValidationType(context, validationType, testCode);

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = testCode,
            ValidationType = validationType
        };

        var result = await purchaseCodeValidationService.ValidateAsync(request);

        // All these scenarios should succeed based on our test data setup
        result.IsValid.ShouldBe(true, $"Validation failed for type {validationType}: {result.ErrorMessage}");
        result.ErrorMessage.ShouldBeNull();
    }

    private static async Task SetupTestDataForValidationType(FoodTourContext context, PurchaseCodeValidationType validationType, string testCode) {
        switch (validationType) {
            case PurchaseCodeValidationType.MustExistAndBeActive:
                await context.PurchaseCodes.AddAsync(new PurchaseCode { Code = testCode, IsActive = true });
                break;

            case PurchaseCodeValidationType.MustNotExist:
                // Don't add any booking - code should not exist
                break;

            case PurchaseCodeValidationType.MustExist:
            case PurchaseCodeValidationType.MustExistForCancellation:
                await context.Bookings.AddAsync(new Booking {
                    PurchaseCode = testCode,
                    CustomerEmail = "test@example.com",
                    TourDate = DateTime.UtcNow.AddDays(1),
                    IsCancelled = false
                });
                break;
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task PurchaseCodeValidationService_ThrowsCorrectException_WhenDatabaseConnectionFails() {
        var services = new ServiceCollection();
        
        // Use an invalid connection string to simulate connection failure
        var options = new DbContextOptionsBuilder<FoodTourContext>()
            .UseSqlite("Data Source=invalid_path/nonexistent.db")
            .Options;

        services.AddSingleton(options);
        services.AddScoped<FoodTourContext>();
        services.AddScoped<IPurchaseCodeValidationService, PurchaseCodeValidationService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var purchaseCodeValidationService = scope.ServiceProvider.GetRequiredService<IPurchaseCodeValidationService>();

        var request = new PurchaseCodeValidationRequest {
            PurchaseCode = "TEST123",
            ValidationType = PurchaseCodeValidationType.MustExistAndBeActive
        };

        // This should throw a database connection exception
        await Should.ThrowAsync<Exception>(async () => {
            await purchaseCodeValidationService.ValidateAsync(request);
        });
    }
}