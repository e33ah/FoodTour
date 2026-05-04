namespace FoodTour.ITest.UI.Tests;

[Collection("FoodTour application")]
public class FoodTourApplicationFixtureTests(FoodTourApplicationFixture fixture) {
    [Fact]
    public async Task GetRoot_ReturnsOk_WhenApplicationStarts() {
        using HttpClient sut = new() {
            BaseAddress = fixture.RootUri
        };

        var result = await sut.GetAsync("/", TestContext.Current.CancellationToken);

        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        fixture.RootUri.Host.ShouldBe("127.0.0.1");
        fixture.RootUri.Port.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task InitializeAsync_SeedsSampleData_WhenFixtureStarts() {
        var sut = fixture;
        var seedData = sut.SeedData;
        DbContextOptionsBuilder<FoodTourContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(sut.ConnectionString);

        await using var context = new FoodTourContext(optionsBuilder.Options);

        int supplierCount = await context.Suppliers.CountAsync(TestContext.Current.CancellationToken);
        int activeSupplierCount = await context.Suppliers.CountAsync(supplier => supplier.IsActive, TestContext.Current.CancellationToken);
        int bookingCount = await context.Bookings.CountAsync(TestContext.Current.CancellationToken);
        int supplierVisitBookingVisitCount = await context.Visits.CountAsync(
            visit => visit.BookingId == seedData.SupplierVisitBooking.Id,
            TestContext.Current.CancellationToken);
        var partnerBooking = await context.Bookings.SingleAsync(
            booking => booking.PurchaseCode == seedData.PartnerConfirmationBooking.PurchaseCode,
            TestContext.Current.CancellationToken);

        supplierCount.ShouldBe(seedData.SupplierCount);
        activeSupplierCount.ShouldBe(seedData.ActiveSupplierCount);
        bookingCount.ShouldBe(seedData.BookingCount);
        supplierVisitBookingVisitCount.ShouldBe(seedData.VisitCount);
        seedData.InactiveSupplier.IsActive.ShouldBeFalse();
        seedData.BookingSelectionBooking.IsConfirmed.ShouldBeFalse();
        seedData.ConfirmedBooking.IsConfirmed.ShouldBeTrue();
        seedData.CancelledBooking.IsCancelled.ShouldBeTrue();
        seedData.SupplierVisitBookingVisits.Count.ShouldBe(seedData.VisitCount);
        partnerBooking.RequiresEmailConfirmation.ShouldBe(seedData.PartnerConfirmationBooking.RequiresEmailConfirmation);
        partnerBooking.VoucherNumber.ShouldBe(seedData.PartnerConfirmationBooking.VoucherNumber);
    }

}
