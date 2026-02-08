namespace FoodTour.Test.Controllers;

public class HomeControllerSupplierVisitTests : HomeControllerTestBase {
    [Fact]
    public async Task SupplierVisit_ReturnsViewResult_WithSupplierDetails() {
        string supplierName = "Gourmet Bites";
        int supplierId = 1;
        int bookingId = 1;
        var supplier = TestHelper.CreateSupplier(supplierId, supplierName, true);
        var booking = TestHelper.CreateValidBooking([bookingId]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = new HomeController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.Suppliers.Count.ShouldBe(1);
        model.Suppliers[0].Name.ShouldBe(supplierName);
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToError_WhenBookingNotFound() {
        var sut = new HomeController(TestHelper.CreateInMemoryContext());

        var result = await sut.SupplierVisit(Guid.NewGuid());

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        viewResult.ActionName.ShouldBe(nameof(HomeController.Error));
    }

    [Fact]
    public async Task SupplierVisit_IncludesAllBookingSuppliers_WhenMultipleAssigned() {
        var supplier1 = TestHelper.CreateSupplier(1, "The Rusty Spoon", true);
        var supplier2 = TestHelper.CreateSupplier(2, "Fisherman's Table", true);
        var booking = TestHelper.CreateValidBooking([1, 2]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier1, supplier2], [booking]);
        var sut = new HomeController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.Suppliers.Count.ShouldBe(2);
        model.Suppliers.Select(s => s.Id).OrderBy(i => i).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToError_WhenQrTokenIsEmpty() {
        var sut = new HomeController(TestHelper.CreateInMemoryContext());
        var emptyToken = Guid.Empty;

        var result = await sut.SupplierVisit(emptyToken);

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        viewResult.ActionName.ShouldBe(nameof(HomeController.Error));
    }

    [Fact]
    public async Task SupplierVisit_ReturnsView_WithUnvisitedSuppliersOnly() {
        var suppliers = new[] {
            TestHelper.CreateSupplier(1, "Unvisited Supplier 1", true),
            TestHelper.CreateSupplier(2, "Visited Supplier", true),
            TestHelper.CreateSupplier(3, "Unvisited Supplier 2", true)
        };
        var booking = TestHelper.CreateValidBooking([1, 2, 3]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings(suppliers, [booking]);
        var visit = new Visit { BookingId = booking.Id, SupplierId = 2, IsClaimed = true, ClaimedAt = DateTime.UtcNow };
        context.Visits.Add(visit);
        context.SaveChanges();
        var sut = new HomeController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.Suppliers.Count.ShouldBe(2);
        model.Suppliers.ShouldContain(s => s.Id == 1);
        model.Suppliers.ShouldContain(s => s.Id == 3);
        model.Suppliers.ShouldNotContain(s => s.Id == 2);
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToError_WhenAllSuppliersAlreadyVisited() {
        var suppliers = new[] {
            TestHelper.CreateSupplier(1, "Supplier 1", true),
            TestHelper.CreateSupplier(2, "Supplier 2", true)
        };
        var booking = TestHelper.CreateValidBooking([1, 2]);
        booking.Id = 1;
        var context = TestHelper.CreateContextWithSuppliersAndBookings(suppliers, [booking]);
        var visit1 = new Visit { BookingId = booking.Id, SupplierId = 1, IsClaimed = true, ClaimedAt = DateTime.UtcNow };
        var visit2 = new Visit { BookingId = booking.Id, SupplierId = 2, IsClaimed = true, ClaimedAt = DateTime.UtcNow };
        context.Visits.AddRange(visit1, visit2);
        context.SaveChanges();
        var sut = CreateControllerWithTempData(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.Error));
        sut.TempData["VisitMessage"].ShouldNotBeNull();
        string? visitMessage = sut.TempData["VisitMessage"]?.ToString();
        visitMessage.ShouldNotBeNull();
        visitMessage.ShouldContain("already confirmed");
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToError_WhenBookingIsCancelled() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var cancelledBooking = TestHelper.CreateValidBooking([1]);
        cancelledBooking.IsCancelled = true;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [cancelledBooking]);
        var sut = new HomeController(context);

        var result = await sut.SupplierVisit(cancelledBooking.QrToken);

        var redirectResult = result as RedirectToActionResult;
        redirectResult.ShouldNotBeNull();
        redirectResult.ActionName.ShouldBe("Error");
    }

    [Fact]
    public async Task SupplierVisit_IncludesBookingId_InViewModel() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = new HomeController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.BookingId.ShouldBe(booking.Id);
        model.BookingId.ShouldBeGreaterThan(0);
    }
}