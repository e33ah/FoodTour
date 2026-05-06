namespace FoodTour.ITest.API.Tests.InfrastructureSmoke;

[Collection("FoodTour API application")]
public class ApiApplicationFixtureTests(ApiApplicationFixture fixture) {
    [Fact]
    public async Task CreateApiClient_GetRoot_ReturnsOk_WhenApplicationStarts() {
        using var sut = fixture.CreateApiClient();

        var result = await sut.GetAsync("/", TestContext.Current.CancellationToken);

        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InitializeAsync_SeedsSampleData_WhenFixtureStarts() {
        var sut = fixture;
        var seedData = sut.SeedData;
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(sut.ConnectionString);

        await using FoodTourContext context = new(optionsBuilder.Options);

        int supplierCount = await context.Suppliers.CountAsync(TestContext.Current.CancellationToken);
        int activeSupplierCount = await context.Suppliers.CountAsync(supplier => supplier.IsActive, TestContext.Current.CancellationToken);
        int bookingCount = await context.Bookings.CountAsync(TestContext.Current.CancellationToken);
        int supplierVisitBookingVisitCount = await context.Visits.CountAsync(
            visit => visit.BookingId == seedData.SupplierVisitBooking.Id,
            TestContext.Current.CancellationToken);

        supplierCount.ShouldBe(seedData.SupplierCount);
        activeSupplierCount.ShouldBe(seedData.ActiveSupplierCount);
        bookingCount.ShouldBeGreaterThan(seedData.BookingCount - 1);
        supplierVisitBookingVisitCount.ShouldBe(seedData.VisitCount);
    }
}
