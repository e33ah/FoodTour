namespace FoodTour.Test.Controllers;

public class HomeControllerSupplierVisitTests : HomeControllerTestBase {
    [Fact]
    public async Task SupplierVisit_ReturnsViewResult_WithSupplierDetails() {
        string supplierName = "Gourmet Bites";
        string foodTourOfferDescription = "1 small tasting plate per guest";
        int supplierId = 1;
        int bookingId = 1;
        var supplier = TestHelper.CreateSupplier(supplierId, supplierName, true, foodTourOfferDescription: foodTourOfferDescription);
        var booking = TestHelper.CreateValidBooking([bookingId]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.Suppliers.Count.ShouldBe(1);
        model.Suppliers[0].Name.ShouldBe(supplierName);
        model.Suppliers[0].FoodTourOfferDescription.ShouldBe(foodTourOfferDescription);
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToError_WhenBookingNotFound() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

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
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.Suppliers.Count.ShouldBe(2);
        model.Suppliers.Select(s => s.Id).OrderBy(i => i).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToError_WhenQrTokenIsEmpty() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());
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
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.Suppliers.Count.ShouldBe(2);
        model.Suppliers.ShouldContain(s => s.Id == 1);
        model.Suppliers.ShouldContain(s => s.Id == 3);
        model.Suppliers.ShouldNotContain(s => s.Id == 2);
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToTourComplete_WhenAllSuppliersAlreadyVisited() {
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
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.TourCompleted));
    }

    [Fact]
    public async Task SupplierVisit_RedirectsToTourCancelled_WhenBookingIsCancelled() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var cancelledBooking = TestHelper.CreateValidBooking([1]);
        cancelledBooking.IsCancelled = true;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [cancelledBooking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(cancelledBooking.QrToken);

        var redirectResult = result as RedirectToActionResult;
        redirectResult.ShouldNotBeNull();
        redirectResult.ActionName.ShouldBe("TourCancelled");
    }

    [Fact]
    public async Task SupplierVisit_IncludesBookingId_InViewModel() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.BookingId.ShouldBe(booking.Id);
        model.BookingId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SupplierVisit_Post_ReturnsViewWithError_WhenSupplierCodeRequiredAndMissing() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        supplier.RequiresConfirmationCode = true;
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, string.Empty, booking.TotalGuests);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBeNull();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage).ShouldContain("Invalid business code provided. Please try again or contact Support.");
    }

    [Fact]
    public async Task SupplierVisit_Post_RedirectsToVisitConfirmation_WhenSupplierCodeOptionalAndMissing() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        supplier.RequiresConfirmationCode = false;
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, string.Empty, booking.TotalGuests);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.VisitConfirmation));
        redirectResult.RouteValues!["claimed"].ShouldBe(booking.TotalGuests);
        redirectResult.RouteValues["total"].ShouldBe(booking.TotalGuests);
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenBookingNotFound_RedirectsToError_Works() {
        var sut = CreateConfiguredController(TestHelper.CreateInMemoryContext());

        var result = await sut.SupplierVisit(Guid.NewGuid(), 1, "CODE1", 1);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.Error));
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenBookingCancelled_RedirectsToTourCancelled_Works() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.IsCancelled = true;
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 1);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.TourCancelled));
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenAllSuppliersClaimed_RedirectsToTourCompleted_Works() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = true, ClaimedAt = DateTime.UtcNow });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 1);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.TourCompleted));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task SupplierVisit_Post_WhenTourDateNotToday_RedirectsToSupplierVisit_Works(int dayOffset) {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today.AddDays(dayOffset);
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 1);

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(HomeController.SupplierVisit));
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["qrToken"].ShouldBe(booking.QrToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task SupplierVisit_Post_WhenSelectedSupplierInvalid_ReturnsErrorView_Works(int selectedSupplierId) {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, selectedSupplierId, supplier.ConfirmationCode, 1);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage).ShouldContain("Please select a valid business to confirm the visit.");
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenSelectedSupplierInactive_ReturnsErrorView_Works() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", false);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 1);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage).ShouldContain("Please select a valid business to confirm the visit.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SupplierVisit_Post_WhenTastingExperiencesInvalid_ReturnsErrorView_Works(int tastingExperiencesClaimed) {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, supplier.ConfirmationCode, tastingExperiencesClaimed);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage).ShouldContain("Number of tasting experiences must be at least 1.");
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenTastingExperiencesExceedTotalGuests_ReturnsErrorView_Works() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        booking.TotalGuests = 3;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 4);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        var errorMessages = sut.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage);
        errorMessages.ShouldContain(message => message.Contains("Cannot claim 4 tasting experiences"));
        errorMessages.ShouldContain(message => message.Contains("This booking has only 3 guest(s)"));
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenVisitMissing_ReturnsErrorView_Works() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, supplier.ConfirmationCode, 1);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage).ShouldContain("Visit not found for this booking and business. Please contact Support.");
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenInvalidCode_PreservesSelectedValuesInViewModel_Works() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        supplier.RequiresConfirmationCode = true;
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, "WRONG_CODE", 1);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SupplierVisitVM>();
        model.SelectedSupplierId.ShouldBe(supplier.Id);
        model.SupplierCode.ShouldBe("WRONG_CODE");
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenRequestValid_UpdatesClaimFields_Works() {
        var supplier = TestHelper.CreateSupplier(1, "Test Supplier", true);
        supplier.RequiresConfirmationCode = false;
        var booking = TestHelper.CreateValidBooking([1]);
        booking.TourDate = DateTime.Today;
        booking.TotalGuests = 5;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);
        var beforeClaimedAt = DateTime.UtcNow.AddSeconds(-1);

        var result = await sut.SupplierVisit(booking.QrToken, supplier.Id, string.Empty, 4);

        result.ShouldBeOfType<RedirectToActionResult>();
        context.ChangeTracker.Clear();
        var updatedVisit = context.Visits.Single(v => v.BookingId == booking.Id && v.SupplierId == supplier.Id);
        updatedVisit.IsClaimed.ShouldBeTrue();
        updatedVisit.TastingExperiencesClaimed.ShouldBe(4);
        updatedVisit.ClaimedAt.ShouldNotBeNull();
        updatedVisit.ClaimedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeClaimedAt);
    }

    [Fact]
    public async Task SupplierVisit_Post_WhenMultipleSuppliersClaimFullGuestCountEach_Works() {
        var supplier1 = TestHelper.CreateSupplier(1, "Supplier One", true);
        var supplier2 = TestHelper.CreateSupplier(2, "Supplier Two", true);
        supplier1.RequiresConfirmationCode = false;
        supplier2.RequiresConfirmationCode = false;
        var booking = TestHelper.CreateValidBooking([1, 2]);
        booking.TourDate = DateTime.Today;
        booking.TotalGuests = 5;
        var context = TestHelper.CreateContextWithSuppliersAndBookings([supplier1, supplier2], [booking]);
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier1.Id, IsClaimed = false });
        context.Visits.Add(new Visit { BookingId = booking.Id, SupplierId = supplier2.Id, IsClaimed = false });
        context.SaveChanges();
        var sut = CreateConfiguredController(context);

        await sut.SupplierVisit(booking.QrToken, supplier1.Id, string.Empty, 5);
        var result = await sut.SupplierVisit(booking.QrToken, supplier2.Id, string.Empty, 5);

        result.ShouldBeOfType<RedirectToActionResult>();
        context.ChangeTracker.Clear();
        var firstVisit = context.Visits.Single(v => v.BookingId == booking.Id && v.SupplierId == supplier1.Id);
        var secondVisit = context.Visits.Single(v => v.BookingId == booking.Id && v.SupplierId == supplier2.Id);
        firstVisit.TastingExperiencesClaimed.ShouldBe(5);
        secondVisit.TastingExperiencesClaimed.ShouldBe(5);
    }
}