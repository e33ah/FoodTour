namespace FoodTour.Test.Controllers;

public class AdminControllerBookingsTests : AdminControllerTestBase {
    [Fact]
    public async Task Bookings_ReturnsView() {
        var sut = CreateController();

        var result = await sut.Bookings();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Bookings_ReturnsAllBookings_WhenNoFilterApplied() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "PAST", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-1) },
            new Booking { PurchaseCode = "TODAY", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today },
            new Booking { PurchaseCode = "FUTR1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(1) },
            new Booking { PurchaseCode = "CANCL", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today, IsCancelled = true });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings();

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.Bookings.Count.ShouldBe(4);
        vm.ActiveFilter.ShouldBeNull();
    }

    [Theory]
    [InlineData("upcoming", 3)]
    [InlineData("past", 1)]
    [InlineData("cancelled", 1)]
    public async Task Bookings_ReturnsFilteredBookings_WhenFilterApplied(string filter, int expectedCount) {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "PAST", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-1) },
            new Booking { PurchaseCode = "TODAY", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today },
            new Booking { PurchaseCode = "FUTR1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(1) },
            new Booking { PurchaseCode = "FUTR2", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(7) },
            new Booking { PurchaseCode = "CANCL", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today, IsCancelled = true });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings(filter);

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.Bookings.Count.ShouldBe(expectedCount);
        vm.ActiveFilter.ShouldBe(filter);
    }

    [Theory]
    [InlineData(AdminBookingsVM.SORT_DATE, "asc", new[] { "OLD", "MID", "NEW" })]
    [InlineData(AdminBookingsVM.SORT_DATE, "desc", new[] { "NEW", "MID", "OLD" })]
    [InlineData(AdminBookingsVM.SORT_REF, "asc", new[] { "MID", "NEW", "OLD" })]
    [InlineData(AdminBookingsVM.SORT_REF, "desc", new[] { "OLD", "NEW", "MID" })]
    [InlineData(AdminBookingsVM.SORT_GUESTS, "asc", new[] { "OLD", "MID", "NEW" })]
    [InlineData(AdminBookingsVM.SORT_GUESTS, "desc", new[] { "NEW", "MID", "OLD" })]
    [InlineData(AdminBookingsVM.SORT_CUSTOMER, "asc", new[] { "MID", "NEW", "OLD" })]
    [InlineData(AdminBookingsVM.SORT_CUSTOMER, "desc", new[] { "OLD", "NEW", "MID" })]
    public async Task Bookings_ReturnsCorrectOrder_WhenSortApplied(string sort, string dir, string[] expectedOrder) {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "OLD", CustomerEmail = TestHelper.VALID_EMAIL, CustomerName = "Charlie", TourDate = today.AddDays(-7), TotalGuests = 1 },
            new Booking { PurchaseCode = "NEW", CustomerEmail = TestHelper.VALID_EMAIL, CustomerName = "Bob", TourDate = today.AddDays(7), TotalGuests = 3 },
            new Booking { PurchaseCode = "MID", CustomerEmail = TestHelper.VALID_EMAIL, CustomerName = "Alice", TourDate = today, TotalGuests = 2 });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings(sort: sort, dir: dir);

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.Bookings.Select(b => b.PurchaseCode).ShouldBe(expectedOrder);
        vm.SortColumn.ShouldBe(sort);
        vm.SortDescending.ShouldBe(dir == "desc");
    }

    [Fact]
    public async Task Bookings_DefaultsToDateDescending_WhenNoSortSpecified() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "OLD", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-7) },
            new Booking { PurchaseCode = "NEW", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(7) });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings();

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.Bookings[0].PurchaseCode.ShouldBe("NEW");
        vm.SortColumn.ShouldBe(AdminBookingsVM.SORT_DATE);
        vm.SortDescending.ShouldBeTrue();
    }

    [Fact]
    public async Task Bookings_FallsBackToDateDescending_WhenSortValueIsUnrecognised() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "OLD", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-7) },
            new Booking { PurchaseCode = "NEW", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(7) });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings(sort: "invalid", dir: "asc");

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.Bookings[0].PurchaseCode.ShouldBe("OLD");
        vm.SortColumn.ShouldBe("invalid");
        vm.SortDescending.ShouldBeFalse();
    }

    [Fact]
    public async Task Bookings_ReturnsBookingsOrderedByDateDescending() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "OLD", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-7) },
            new Booking { PurchaseCode = "NEW", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(7) },
            new Booking { PurchaseCode = "MID", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings();

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.Bookings[0].PurchaseCode.ShouldBe("NEW");
        vm.Bookings[1].PurchaseCode.ShouldBe("MID");
        vm.Bookings[2].PurchaseCode.ShouldBe("OLD");
    }

    [Fact]
    public async Task Bookings_ReturnsTotalCount_ReflectingAllMatchingBookings() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(Enumerable.Range(1, 30).Select(i =>
            new Booking { PurchaseCode = $"CODE{i:D2}", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(i) }));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings();

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.TotalCount.ShouldBe(30);
        vm.Bookings.Count.ShouldBe(25);
        vm.TotalPages.ShouldBe(2);
        vm.HasNextPage.ShouldBeTrue();
        vm.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Bookings_ReturnsCorrectItems_WhenOnSecondPage() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(Enumerable.Range(1, 30).Select(i =>
            new Booking { PurchaseCode = $"CODE{i:D2}", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(i) }));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings(page: 2);

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.Bookings.Count.ShouldBe(5);
        vm.CurrentPage.ShouldBe(2);
        vm.HasPreviousPage.ShouldBeTrue();
        vm.HasNextPage.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Bookings_ClampsToPageOne_WhenPageIsZeroOrNegative(int invalidPage) {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.Add(new Booking { PurchaseCode = "TEST", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings(page: invalidPage);

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.CurrentPage.ShouldBe(1);
        vm.Bookings.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Bookings_ClampsToLastPage_WhenPageExceedsTotalPages() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(Enumerable.Range(1, 30).Select(i =>
            new Booking { PurchaseCode = $"CODE{i:D2}", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(i) }));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings(page: 9999);

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.CurrentPage.ShouldBe(2);
        vm.Bookings.Count.ShouldBe(5);
    }

    [Theory]
    [InlineData("upcoming", 3)]
    [InlineData("past", 1)]
    [InlineData("cancelled", 1)]
    public async Task Bookings_TotalCountReflectsFilteredTotal_WhenFilterApplied(string filter, int expectedCount) {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        context.Bookings.AddRange(
            new Booking { PurchaseCode = "PAST", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(-1) },
            new Booking { PurchaseCode = "TODAY", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today },
            new Booking { PurchaseCode = "FUTR1", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(1) },
            new Booking { PurchaseCode = "FUTR2", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today.AddDays(7) },
            new Booking { PurchaseCode = "CANCL", CustomerEmail = TestHelper.VALID_EMAIL, TourDate = today, IsCancelled = true });
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Bookings(filter);

        var vm = result.Model.ShouldBeOfType<AdminBookingsVM>();
        vm.TotalCount.ShouldBe(expectedCount);
    }

    [Fact]
    public async Task BookingDetail_ReturnsNotFound_WhenBookingDoesNotExist() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateController(context: context);

        var result = await sut.BookingDetail(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task BookingDetail_ReturnsView_WithCorrectBooking() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.BookingDetail(booking.Id);

        var vm = result.Model.ShouldBeOfType<AdminBookingDetailVM>();
        vm.Booking.Id.ShouldBe(booking.Id);
        vm.Booking.PurchaseCode.ShouldBe(booking.PurchaseCode);
    }

    [Fact]
    public async Task BookingDetail_ReturnsEmptyVisits_WhenNoVisitsRecorded() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.BookingDetail(booking.Id);

        var vm = result.Model.ShouldBeOfType<AdminBookingDetailVM>();
        vm.Visits.Count.ShouldBe(0);
    }

    [Fact]
    public async Task BookingDetail_IncludesVisitsForBooking() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(
            TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true),
            TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: false));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.BookingDetail(booking.Id);

        var vm = result.Model.ShouldBeOfType<AdminBookingDetailVM>();
        vm.Visits.Count.ShouldBe(2);
    }

    [Fact]
    public async Task BookingDetail_ExcludesVisitsForOtherBookings() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking1 = TestHelper.CreateValidBooking([supplier.Id]);
        var booking2 = TestHelper.CreateValidBooking([supplier.Id]);
        booking2.PurchaseCode = "OTHER_CODE";
        context.Suppliers.Add(supplier);
        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.Add(TestHelper.CreateVisit(booking2.Id, supplier.Id));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.BookingDetail(booking1.Id);

        var vm = result.Model.ShouldBeOfType<AdminBookingDetailVM>();
        vm.Visits.Count.ShouldBe(0);
    }

    [Fact]
    public async Task BookingDetail_IncludesSelectedSuppliers() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier1 = TestHelper.CreateSupplier(1);
        var supplier2 = TestHelper.CreateSupplier(2);
        var booking = TestHelper.CreateValidBooking([supplier1.Id, supplier2.Id]);
        context.Suppliers.AddRange(supplier1, supplier2);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.BookingDetail(booking.Id);

        var vm = result.Model.ShouldBeOfType<AdminBookingDetailVM>();
        vm.SelectedSuppliers.Count.ShouldBe(2);
    }

    [Fact]
    public async Task BookingDetail_ReturnsEmptySelectedSuppliers_WhenNoSuppliersBooked() {
        var context = TestHelper.CreateInMemoryContext();
        var booking = TestHelper.CreateValidBooking([]);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.BookingDetail(booking.Id);

        var vm = result.Model.ShouldBeOfType<AdminBookingDetailVM>();
        vm.SelectedSuppliers.Count.ShouldBe(0);
    }
}
