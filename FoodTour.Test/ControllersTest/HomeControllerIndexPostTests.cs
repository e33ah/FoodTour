namespace FoodTour.Test.Controllers;

public class HomeControllerIndexPostTests : HomeControllerTestBase {
    
    [Fact]
    public async Task Index_ReturnsBadRequest_WhenBookingIdIsZero() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(0, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("Your booking could not be validated. Please enter your email and booking reference.");
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenBookingIdIsNegative() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(-1, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("Your booking could not be validated. Please enter your email and booking reference.");
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenBookingNotFound() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(999, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("Your booking could not be found or has been cancelled. Please verify your details.");
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenBookingIsCancelled() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        booking.IsCancelled = true;
        await context.SaveChangesAsync(CancellationToken);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(booking.Id, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("Your booking could not be found or has been cancelled. Please verify your details.");
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenNoSuppliersSelected() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = new List<int>();

        var result = await sut.Index(booking.Id, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldContain("You must select 5 tasting experiences");
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenTooFewSuppliersSelected() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(3);

        var result = await sut.Index(booking.Id, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldContain("You must select 5 tasting experiences");
    }

    [Fact]
    public async Task Index_ReturnsViewWithTourDate_WhenSupplierSelectionsAreInvalid() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        DateTime tourDate = new(2026, 4, 21);
        var booking = await TestHelper.CreateBookingInDatabase(context, tourDate: tourDate);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(3);

        var result = await sut.Index(booking.Id, selectedSuppliers);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<FoodTourVM>();

        model.TourDate.ShouldBe(tourDate);
        model.SelectedSuppliers.ShouldBe(selectedSuppliers);
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenTooManySuppliersSelected() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(7);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(7);

        var result = await sut.Index(booking.Id, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldContain("You must select 5 tasting experiences");
    }

    [Fact]
    public async Task Index_ReturnsBadRequest_WhenSupplierIdsAreInvalid() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = new List<int> { 1, 2, 3, 999, 1000 };

        var result = await sut.Index(booking.Id, selectedSuppliers);

        result.ShouldBeOfType<ViewResult>();
        sut.ModelState.IsValid.ShouldBeFalse();
        sut.ModelState[string.Empty]!.Errors.ShouldHaveSingleItem();
        sut.ModelState[string.Empty]!.Errors[0].ErrorMessage.ShouldBe("One or more selected suppliers are invalid. Please refresh and try again.");
    }

    [Fact]
    public async Task Index_UpdatesBooking_WhenAllValidationsPassed() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        var result = await sut.Index(booking.Id, selectedSuppliers);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(HomeController.BookingSummary));
        redirect.RouteValues.ShouldNotBeNull();
        redirect.RouteValues["qrToken"].ShouldBe(booking.QrToken);

        var updatedBooking = await context.Bookings.FindAsync([booking.Id], CancellationToken);
        updatedBooking.ShouldNotBeNull();
        updatedBooking.SelectedSupplierIds.ShouldBe(selectedSuppliers);
    }

    [Fact]
    public async Task Index_CreatesBookingSuppliers_WhenBookingUpdated() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        await sut.Index(booking.Id, selectedSuppliers);

        var bookingSuppliers = await context.BookingSuppliers
            .Where(bs => bs.BookingId == booking.Id)
            .ToListAsync(CancellationToken);
        
        bookingSuppliers.Count.ShouldBe(5);
        
        foreach (int supplierId in selectedSuppliers) {
            bookingSuppliers.Any(bs => bs.SupplierId == supplierId).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Index_CreatesVisits_WhenBookingUpdated() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(5);
        var booking = await TestHelper.CreateBookingInDatabase(context);
        var sut = CreateConfiguredController(context);
        var selectedSuppliers = TestHelper.CreateSupplierIdList(5);

        await sut.Index(booking.Id, selectedSuppliers);

        var visits = await context.Visits
            .Where(v => v.BookingId == booking.Id)
            .ToListAsync(CancellationToken);
        
        visits.Count.ShouldBe(5);
        
        foreach (int supplierId in selectedSuppliers) {
            var visit = visits.FirstOrDefault(v => v.SupplierId == supplierId);
            visit.ShouldNotBeNull();
            visit.IsClaimed.ShouldBeFalse();
            visit.ClaimedAt.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Index_RemovesOldSuppliers_WhenBookingUpdatedAgain() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(7);
        var booking = await TestHelper.CreateBookingInDatabase(context, selectedSupplierIds: [1, 2, 3, 4, 5]);
        
        var oldBookingSuppliers = new List<BookingSupplier> {
            new() { BookingId = booking.Id, SupplierId = 1 },
            new() { BookingId = booking.Id, SupplierId = 2 },
            new() { BookingId = booking.Id, SupplierId = 3 },
            new() { BookingId = booking.Id, SupplierId = 4 },
            new() { BookingId = booking.Id, SupplierId = 5 }
        };
        await context.BookingSuppliers.AddRangeAsync(oldBookingSuppliers, CancellationToken);
        await context.SaveChangesAsync(CancellationToken);

        var sut = CreateConfiguredController(context);
        var newSelectedSuppliers = new List<int> { 2, 3, 4, 6, 7 };

        await sut.Index(booking.Id, newSelectedSuppliers);

        var bookingSuppliers = await context.BookingSuppliers
            .Where(bs => bs.BookingId == booking.Id)
            .ToListAsync(CancellationToken);
        
        bookingSuppliers.Count.ShouldBe(5);
        bookingSuppliers.Any(bs => bs.SupplierId == 1).ShouldBeFalse();
        bookingSuppliers.Any(bs => bs.SupplierId == 5).ShouldBeFalse();
        bookingSuppliers.Any(bs => bs.SupplierId == 6).ShouldBeTrue();
        bookingSuppliers.Any(bs => bs.SupplierId == 7).ShouldBeTrue();
    }

    [Fact]
    public async Task Index_RemovesOldVisits_WhenBookingUpdatedAgain() {
        var context = TestHelper.CreateContextWithActiveSuppliersCount(7);
        var booking = await TestHelper.CreateBookingInDatabase(context, selectedSupplierIds: [1, 2, 3, 4, 5]);
        
        var oldVisits = new List<Visit> {
            new() { BookingId = booking.Id, SupplierId = 1 },
            new() { BookingId = booking.Id, SupplierId = 2 },
            new() { BookingId = booking.Id, SupplierId = 3 },
            new() { BookingId = booking.Id, SupplierId = 4 },
            new() { BookingId = booking.Id, SupplierId = 5 }
        };
        await context.Visits.AddRangeAsync(oldVisits, CancellationToken);
        await context.SaveChangesAsync(CancellationToken);

        var sut = CreateConfiguredController(context);
        var newSelectedSuppliers = new List<int> { 2, 3, 4, 6, 7 };

        await sut.Index(booking.Id, newSelectedSuppliers);

        var visits = await context.Visits
            .Where(v => v.BookingId == booking.Id)
            .ToListAsync(CancellationToken);
        
        visits.Count.ShouldBe(5);
        visits.Any(v => v.SupplierId == 1).ShouldBeFalse();
        visits.Any(v => v.SupplierId == 5).ShouldBeFalse();
        visits.Any(v => v.SupplierId == 6).ShouldBeTrue();
        visits.Any(v => v.SupplierId == 7).ShouldBeTrue();
    }
}
