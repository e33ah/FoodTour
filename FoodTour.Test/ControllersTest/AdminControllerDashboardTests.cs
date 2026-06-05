namespace FoodTour.Test.Controllers;

public class AdminControllerDashboardTests : AdminControllerTestBase {
    [Fact]
    public async Task Dashboard_ReturnsView_WithViewModel() {
        var sut = CreateController();

        var result = await sut.Dashboard();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Dashboard_ReturnsSummaryWithZeroCounts_WhenDatabaseIsEmpty() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Dashboard();

        var viewModel = result.Model.ShouldBeOfType<AdminDashboardVM>();
        viewModel.PastBookings.ShouldBe(0);
        viewModel.TodayBookings.ShouldBe(0);
        viewModel.UpcomingBookings.ShouldBe(0);
        viewModel.VisitsAwaitingReimbursement.ShouldBe(0);
        viewModel.ActiveSuppliers.ShouldBe(0);
    }

    [Fact]
    public async Task Dashboard_ReturnsTodayBookingCount_ExcludingCancelledAndOtherDates() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "TODAY1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddHours(10) },
            new Booking { PurchaseCode = "TODAY2", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddHours(14) },
            new Booking { PurchaseCode = "CANCEL", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddHours(11), IsCancelled = true },
            new Booking { PurchaseCode = "FUTURE", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(1) },
            new Booking { PurchaseCode = "PAST1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-1) });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Dashboard();

        var viewModel = result.Model.ShouldBeOfType<AdminDashboardVM>();
        viewModel.TodayBookings.ShouldBe(2);
    }

    [Fact]
    public async Task Dashboard_ReturnsUpcomingBookingCount_ExcludingTodayAndCancelled() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "FUT1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(1) },
            new Booking { PurchaseCode = "FUT2", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(7) },
            new Booking { PurchaseCode = "CANCEL", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(2), IsCancelled = true },
            new Booking { PurchaseCode = "TODAY1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddHours(10) },
            new Booking { PurchaseCode = "PAST1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-1) });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Dashboard();

        var viewModel = result.Model.ShouldBeOfType<AdminDashboardVM>();
        viewModel.UpcomingBookings.ShouldBe(2);
    }

    [Fact]
    public async Task Dashboard_ReturnsPastBookingCount_ExcludingCancelledAndFutureDates() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "PAST1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-1) },
            new Booking { PurchaseCode = "PAST2", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-7) },
            new Booking { PurchaseCode = "CANCEL", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-2), IsCancelled = true },
            new Booking { PurchaseCode = "TODAY1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddHours(10) },
            new Booking { PurchaseCode = "FUTURE", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(1) });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Dashboard();

        var viewModel = result.Model.ShouldBeOfType<AdminDashboardVM>();
        viewModel.PastBookings.ShouldBe(2);
    }

    [Fact]
    public async Task Dashboard_ReturnsAwaitingReimbursementVisitCount_WhenVisitClaimedButNotReimbursed() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([1]);
        var supplier = TestHelper.CreateSupplier(1);
        context.Bookings.Add(booking);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(
            TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true, isReimbursed: false),
            TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true, isReimbursed: true),
            TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: false, isReimbursed: false));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Dashboard();

        var viewModel = result.Model.ShouldBeOfType<AdminDashboardVM>();
        viewModel.VisitsAwaitingReimbursement.ShouldBe(1);
    }

    [Fact]
    public async Task Dashboard_ReturnsActiveSupplierCount_ExcludingInactive() {
        var context = TestHelper.CreateInMemoryContext();
        context.Suppliers.AddRange(
            TestHelper.CreateSupplier(1, isActive: true),
            TestHelper.CreateSupplier(2, isActive: true),
            TestHelper.CreateSupplier(3, isActive: false));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Dashboard();

        var viewModel = result.Model.ShouldBeOfType<AdminDashboardVM>();
        viewModel.ActiveSuppliers.ShouldBe(2);
    }
}
