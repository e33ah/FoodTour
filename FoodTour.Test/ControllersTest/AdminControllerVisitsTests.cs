namespace FoodTour.Test.Controllers;

public class AdminControllerVisitsTests : AdminControllerTestBase {
    [Fact]
    public async Task Visits_ReturnsView() {
        var sut = CreateController();

        var result = await sut.Visits();

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task Visits_ReturnsAllVisits_WhenNoFilterApplied() {
        var (context, _, _) = await TestHelper.CreateContextWithVisitsAsync([
            TestHelper.CreateVisit(isClaimed: true),
            TestHelper.CreateVisit(isClaimed: false)
        ], CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits();

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits.Count.ShouldBe(2);
        vm.ActiveFilter.ShouldBeNull();
    }

    [Theory]
    [InlineData("claimed", 1)]
    [InlineData("unclaimed", 1)]
    public async Task Visits_ReturnsFilteredVisits_WhenClaimedFilterApplied(string filter, int expectedCount) {
        var (context, _, _) = await TestHelper.CreateContextWithVisitsAsync([
            TestHelper.CreateVisit(isClaimed: true),
            TestHelper.CreateVisit(isClaimed: false)
        ], CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits(filter);

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits.Count.ShouldBe(expectedCount);
        vm.ActiveFilter.ShouldBe(filter);
    }

    [Fact]
    public async Task Visits_ReturnsOnlyReimbursed_WhenReimbursedFilterApplied() {
        var (context, _, _) = await TestHelper.CreateContextWithVisitsAsync([
            TestHelper.CreateVisit(isClaimed: true, isReimbursed: true),
            TestHelper.CreateVisit(isClaimed: true, isReimbursed: false),
            TestHelper.CreateVisit(isClaimed: false, isReimbursed: false)
        ], CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits("reimbursed");

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits.Count.ShouldBe(1);
        vm.Visits[0].IsSupplierReimbursed.ShouldBeTrue();
    }

    [Fact]
    public async Task Visits_ReturnsOnlyClaimedUnreimbursed_WhenUnreimbursedFilterApplied() {
        var (context, _, _) = await TestHelper.CreateContextWithVisitsAsync([
            TestHelper.CreateVisit(isClaimed: true, isReimbursed: false),
            TestHelper.CreateVisit(isClaimed: true, isReimbursed: true),
            TestHelper.CreateVisit(isClaimed: false, isReimbursed: false)
        ], CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits("unreimbursed");

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits.Count.ShouldBe(1);
        vm.Visits[0].IsClaimed.ShouldBeTrue();
        vm.Visits[0].IsSupplierReimbursed.ShouldBeFalse();
    }

    [Fact]
    public async Task MarkReimbursed_ReturnsNotFound_WhenVisitDoesNotExist() {
        var context = TestHelper.CreateInMemoryContext();
        var sut = CreateController(context: context);

        var result = await sut.MarkReimbursed(999);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkReimbursed_SetsIsSupplierReimbursed_WhenVisitIsNotReimbursed() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var visit = TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true, isReimbursed: false);
        context.Visits.Add(visit);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        await sut.MarkReimbursed(visit.Id);

        var updated = await context.Visits.FindAsync([visit.Id], CancellationToken);
        updated!.IsSupplierReimbursed.ShouldBeTrue();
        updated.ReimbursedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkReimbursed_SetsReimbursedAt_WhenVisitIsNotReimbursed() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var visit = TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true, isReimbursed: false);
        context.Visits.Add(visit);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var before = DateTime.UtcNow;
        await sut.MarkReimbursed(visit.Id);
        var after = DateTime.UtcNow;

        var updated = await context.Visits.FindAsync([visit.Id], CancellationToken);
        updated!.ReimbursedAt.ShouldNotBeNull();
        updated.ReimbursedAt!.Value.ShouldBeInRange(before, after);
    }

    [Fact]
    public async Task MarkReimbursed_ClearsIsSupplierReimbursed_WhenVisitIsAlreadyReimbursed() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var visit = TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true, isReimbursed: true);
        context.Visits.Add(visit);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        await sut.MarkReimbursed(visit.Id);

        var updated = await context.Visits.FindAsync([visit.Id], CancellationToken);
        updated!.IsSupplierReimbursed.ShouldBeFalse();
        updated.ReimbursedAt.ShouldBeNull();
    }

    [Fact]
    public async Task MarkReimbursed_RedirectsToVisits_WithNoFilter_WhenNoFilterProvided() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var visit = TestHelper.CreateVisit(booking.Id, supplier.Id);
        context.Visits.Add(visit);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = await sut.MarkReimbursed(visit.Id);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.Visits));
        redirect.RouteValues!["filter"].ShouldBeNull();
    }

    [Fact]
    public async Task MarkReimbursed_RedirectsToVisits_PreservingFilter_WhenFilterProvided() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var visit = TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true);
        context.Visits.Add(visit);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = await sut.MarkReimbursed(visit.Id, "unreimbursed");

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AdminController.Visits));
        redirect.RouteValues!["filter"].ShouldBe("unreimbursed");
    }

    [Fact]
    public async Task MarkReimbursed_ReturnsJsonSuccess_WhenAjaxRequest() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var visit = TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true, isReimbursed: false);
        context.Visits.Add(visit);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);
        sut.ControllerContext.HttpContext.Request.Headers.XRequestedWith = "XMLHttpRequest";

        var result = await sut.MarkReimbursed(visit.Id);

        var jsonResult = result.ShouldBeOfType<JsonResult>();
        var value = jsonResult.Value.ShouldBeAssignableTo<dynamic>();
        ((bool)value!.success).ShouldBeTrue();
        ((bool)value!.isReimbursed).ShouldBeTrue();
        ((string)value!.reimbursedAt).ShouldNotBeNullOrEmpty();
        ((string)value!.message).ShouldBe("Marked as reimbursed.");
    }

    [Fact]
    public async Task MarkReimbursed_ReturnsJsonWithUndoMessage_WhenAjaxRequestTogglesOff() {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        var visit = TestHelper.CreateVisit(booking.Id, supplier.Id, isClaimed: true, isReimbursed: true);
        context.Visits.Add(visit);
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);
        sut.ControllerContext.HttpContext.Request.Headers.XRequestedWith = "XMLHttpRequest";

        var result = await sut.MarkReimbursed(visit.Id);

        var jsonResult = result.ShouldBeOfType<JsonResult>();
        var value = jsonResult.Value.ShouldBeAssignableTo<dynamic>();
        ((bool)value!.success).ShouldBeTrue();
        ((bool)value!.isReimbursed).ShouldBeFalse();
        ((string)value!.message).ShouldBe("Reimbursement undone.");
    }

    [Theory]
    [InlineData(AdminVisitsVM.SORT_DATE, "asc", new[] { "OLD", "MID", "NEW" })]
    [InlineData(AdminVisitsVM.SORT_DATE, "desc", new[] { "NEW", "MID", "OLD" })]
    [InlineData(AdminVisitsVM.SORT_REF, "asc", new[] { "MID", "NEW", "OLD" })]
    [InlineData(AdminVisitsVM.SORT_REF, "desc", new[] { "OLD", "NEW", "MID" })]
    [InlineData(AdminVisitsVM.SORT_CUSTOMER, "asc", new[] { "MID", "NEW", "OLD" })]
    [InlineData(AdminVisitsVM.SORT_CUSTOMER, "desc", new[] { "OLD", "NEW", "MID" })]
    [InlineData(AdminVisitsVM.SORT_SUPPLIER, "asc", new[] { "MID", "OLD", "NEW" })]
    [InlineData(AdminVisitsVM.SORT_SUPPLIER, "desc", new[] { "NEW", "OLD", "MID" })]
    public async Task Visits_ReturnsCorrectOrder_WhenSortApplied(string sort, string dir, string[] expectedOrder) {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier1 = TestHelper.CreateSupplier(1, "Bravo Supplier");
        var supplier2 = TestHelper.CreateSupplier(2, "Charlie Supplier");
        var supplier3 = TestHelper.CreateSupplier(3, "Alpha Supplier");
        var booking1 = TestHelper.CreateValidBooking([supplier1.Id]);
        booking1.PurchaseCode = "OLD";
        booking1.CustomerName = "Charlie";
        booking1.TourDate = today.AddDays(-7);
        var booking2 = TestHelper.CreateValidBooking([supplier2.Id]);
        booking2.PurchaseCode = "NEW";
        booking2.CustomerName = "Bob";
        booking2.TourDate = today.AddDays(7);
        var booking3 = TestHelper.CreateValidBooking([supplier3.Id]);
        booking3.PurchaseCode = "MID";
        booking3.CustomerName = "Alice";
        booking3.TourDate = today;
        context.Suppliers.AddRange(supplier1, supplier2, supplier3);
        context.Bookings.AddRange(booking1, booking2, booking3);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(
            TestHelper.CreateVisit(booking1.Id, supplier1.Id),
            TestHelper.CreateVisit(booking2.Id, supplier2.Id),
            TestHelper.CreateVisit(booking3.Id, supplier3.Id));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits(sort: sort, dir: dir);

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits.Select(v => v.Booking.PurchaseCode).ShouldBe(expectedOrder);
        vm.SortColumn.ShouldBe(sort);
        vm.SortDescending.ShouldBe(dir == "desc");
    }

    [Fact]
    public async Task Visits_DefaultsToDateDescending_WhenNoSortSpecified() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier = TestHelper.CreateSupplier(1);
        var booking1 = TestHelper.CreateValidBooking([supplier.Id]);
        booking1.PurchaseCode = "OLD";
        booking1.TourDate = today.AddDays(-7);
        var booking2 = TestHelper.CreateValidBooking([supplier.Id]);
        booking2.PurchaseCode = "NEW";
        booking2.TourDate = today.AddDays(7);
        context.Suppliers.Add(supplier);
        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(
            TestHelper.CreateVisit(booking1.Id, supplier.Id),
            TestHelper.CreateVisit(booking2.Id, supplier.Id));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits();

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits[0].Booking.PurchaseCode.ShouldBe("NEW");
        vm.SortColumn.ShouldBe(AdminVisitsVM.SORT_DATE);
        vm.SortDescending.ShouldBeTrue();
    }

    [Fact]
    public async Task Visits_FallsBackToDateDescending_WhenSortValueIsUnrecognised() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier = TestHelper.CreateSupplier(1);
        var booking1 = TestHelper.CreateValidBooking([supplier.Id]);
        booking1.PurchaseCode = "OLD";
        booking1.TourDate = today.AddDays(-7);
        var booking2 = TestHelper.CreateValidBooking([supplier.Id]);
        booking2.PurchaseCode = "NEW";
        booking2.TourDate = today.AddDays(7);
        context.Suppliers.Add(supplier);
        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(
            TestHelper.CreateVisit(booking1.Id, supplier.Id),
            TestHelper.CreateVisit(booking2.Id, supplier.Id));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits(sort: "invalid", dir: "asc");

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits[0].Booking.PurchaseCode.ShouldBe("OLD");
        vm.SortColumn.ShouldBe("invalid");
        vm.SortDescending.ShouldBeFalse();
    }

    [Fact]
    public async Task Visits_ReturnsTotalCount_ReflectingAllMatchingVisits() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier = TestHelper.CreateSupplier(1);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(CancellationToken);
        var bookings = Enumerable.Range(1, 30).Select(i => {
            var booking = TestHelper.CreateValidBooking([supplier.Id]);
            booking.PurchaseCode = $"CODE{i:D2}";
            booking.TourDate = today.AddDays(i);
            return booking;
        }).ToList();
        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(bookings.Select(b => TestHelper.CreateVisit(b.Id, supplier.Id)));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits();

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.TotalCount.ShouldBe(30);
        vm.Visits.Count.ShouldBe(25);
        vm.TotalPages.ShouldBe(2);
        vm.HasNextPage.ShouldBeTrue();
        vm.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Visits_ReturnsCorrectItems_WhenOnSecondPage() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier = TestHelper.CreateSupplier(1);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(CancellationToken);
        var bookings = Enumerable.Range(1, 30).Select(i => {
            var booking = TestHelper.CreateValidBooking([supplier.Id]);
            booking.PurchaseCode = $"CODE{i:D2}";
            booking.TourDate = today.AddDays(i);
            return booking;
        }).ToList();
        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(bookings.Select(b => TestHelper.CreateVisit(b.Id, supplier.Id)));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits(page: 2);

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits.Count.ShouldBe(5);
        vm.CurrentPage.ShouldBe(2);
        vm.HasPreviousPage.ShouldBeTrue();
        vm.HasNextPage.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Visits_ClampsToPageOne_WhenPageIsZeroOrNegative(int invalidPage) {
        var context = TestHelper.CreateInMemoryContext();
        var supplier = TestHelper.CreateSupplier(1);
        var booking = TestHelper.CreateValidBooking([supplier.Id]);
        context.Suppliers.Add(supplier);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.Add(TestHelper.CreateVisit(booking.Id, supplier.Id));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits(page: invalidPage);

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.CurrentPage.ShouldBe(1);
        vm.Visits.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Visits_ClampsToLastPage_WhenPageExceedsTotalPages() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier = TestHelper.CreateSupplier(1);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(CancellationToken);
        var bookings = Enumerable.Range(1, 30).Select(i => {
            var booking = TestHelper.CreateValidBooking([supplier.Id]);
            booking.PurchaseCode = $"CODE{i:D2}";
            booking.TourDate = today.AddDays(i);
            return booking;
        }).ToList();
        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(bookings.Select(b => TestHelper.CreateVisit(b.Id, supplier.Id)));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits(page: 9999);

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.CurrentPage.ShouldBe(2);
        vm.Visits.Count.ShouldBe(5);
    }

    [Theory]
    [InlineData("claimed", 1)]
    [InlineData("unclaimed", 1)]
    public async Task Visits_TotalCountReflectsFilteredTotal_WhenFilterApplied(string filter, int expectedCount) {
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

        var result = (ViewResult)await sut.Visits(filter);

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.TotalCount.ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData(AdminVisitsVM.SORT_GUESTS, "asc", new[] { 2, 4, 6 })]
    [InlineData(AdminVisitsVM.SORT_GUESTS, "desc", new[] { 6, 4, 2 })]
    public async Task Visits_ReturnsCorrectOrder_WhenSortedByGuests(string sort, string dir, int[] expectedGuestCounts) {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier = TestHelper.CreateSupplier(1);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(CancellationToken);
        var booking1 = TestHelper.CreateValidBooking([supplier.Id]);
        booking1.TotalGuests = 4;
        booking1.TourDate = today;
        var booking2 = TestHelper.CreateValidBooking([supplier.Id]);
        booking2.TotalGuests = 2;
        booking2.TourDate = today.AddDays(1);
        var booking3 = TestHelper.CreateValidBooking([supplier.Id]);
        booking3.TotalGuests = 6;
        booking3.TourDate = today.AddDays(2);
        context.Bookings.AddRange(booking1, booking2, booking3);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(
            TestHelper.CreateVisit(booking1.Id, supplier.Id),
            TestHelper.CreateVisit(booking2.Id, supplier.Id),
            TestHelper.CreateVisit(booking3.Id, supplier.Id));
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateController(context: context);

        var result = (ViewResult)await sut.Visits(sort: sort, dir: dir);

        var vm = result.Model.ShouldBeOfType<AdminVisitsVM>();
        vm.Visits.Select(v => v.Booking.TotalGuests).ShouldBe(expectedGuestCounts);
        vm.SortColumn.ShouldBe(sort);
        vm.SortDescending.ShouldBe(dir == "desc");
    }

    [Fact]
    public async Task Visits_RedirectsToPageContainingHighlightedVisit_WhenHighlightProvided() {
        var context = TestHelper.CreateInMemoryContext();
        var today = DateTime.UtcNow.Date;
        var supplier = TestHelper.CreateSupplier(1);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(CancellationToken);
        var bookings = Enumerable.Range(1, 30).Select(i => {
                    var booking = TestHelper.CreateValidBooking([supplier.Id]);
            booking.PurchaseCode = $"CODE{i:D2}";
            booking.TourDate = today.AddDays(i);
            return booking;
        }).ToList();
        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync(CancellationToken);
        context.Visits.AddRange(bookings.Select(b => TestHelper.CreateVisit(b.Id, supplier.Id)));
        await context.SaveChangesAsync(CancellationToken);

        // The earliest booking (CODE01) will be last when sorted by date descending and thus should be on page 2
        var targetBooking = bookings.First();
        var visit = await context.Visits.FirstAsync(v => v.BookingId == targetBooking.Id, CancellationToken);

        var sut = CreateController(context: context);

        var result = await sut.Visits(highlightVisitId: visit.Id);

        result.ShouldBeOfType<RedirectResult>();
        var redirect = (RedirectResult)result;
        redirect.Url.ShouldNotBeNull();
        redirect.Url.ShouldContain($"#visit-{visit.Id}");
    }
}
